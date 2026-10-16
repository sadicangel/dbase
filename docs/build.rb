# Build the API reference and guides together, with shared navigation and search.
require "fileutils"
require "nokogiri"
require "tmpdir"
require "yaml"

repository = File.expand_path("..", __dir__)
artifacts = File.join(repository, "artifacts")
FileUtils.mkdir_p(artifacts)
serve = ARGV.delete("--serve")
abort "Usage: ruby docs/build.rb [--serve]" unless ARGV.empty?

def run!(*command, **options)
  abort "Failed: #{command.join(' ')}" unless system(*command, **options)
end

Dir.chdir(repository) do
  run!("dotnet", "build", "src/DBase/DBase.csproj", "--configuration", "Release")
  run!("dotnet", "tool", "restore")

  # Keep generated Markdown and the adjusted XML out of the source tree.
  Dir.mktmpdir("docs-build-", artifacts) do |scratch|
    source = File.join(scratch, "source")
    assembly = File.join(scratch, "assembly")
    FileUtils.mkdir_p([source, assembly])
    Dir.glob(File.join(__dir__, "{*.md,_config.yml,_includes,assets}")).each do |path|
      FileUtils.cp_r(path, source)
    end
    output = File.join(repository, "src/DBase/bin/Release/net11.0")
    FileUtils.cp(Dir.glob(File.join(output, "*.{dll,xml}")), assembly)

    # C# extension blocks forward docs to compiler-generated members via inheritdoc.
    # xmldocmd 2.9 does not resolve explicit cref targets; expand them in this copy.
    xml_path = File.join(assembly, "DBase.xml")
    xml = Nokogiri::XML(File.read(xml_path))
    members = xml.xpath("/doc/members/member").to_h { |member| [member["name"], member] }
    xml.xpath("//inheritdoc[@cref]").each do |inherit|
      target = members[inherit["cref"]]
      next unless target

      target.children.each { |child| inherit.add_previous_sibling(child.dup) }
      # The compiler stores extension receiver parameter docs on the grouping type.
      group = target["name"].rpartition(".").first.delete_prefix("M:")
      receiver = members.values.find { |member| member["name"].start_with?("T:#{group}.<M>") }
      receiver&.xpath("param")&.each do |param|
        inherit.add_previous_sibling(param.dup) unless inherit.parent.xpath("param").any? { |existing| existing["name"] == param["name"] }
      end
      inherit.remove
    end
    File.write(xml_path, xml.to_xml)

    api = File.join(source, "api")
    # Run on the library's runtime, including preview runtimes while net11 is in preview.
    run!({ "DOTNET_ROLL_FORWARD" => "LatestMajor", "DOTNET_ROLL_FORWARD_TO_PRERELEASE" => "1" },
         "dotnet", "tool", "run", "xmldocmd", File.join(assembly, "DBase.dll"), api,
         "--visibility", "public", "--skip-compiler-generated", "--newline", "lf", "--quiet")
    index = File.join(api, "DBase.md")
    abort "API generator did not produce a type index" unless File.file?(index)
    FileUtils.mv(index, File.join(api, "index.md"))

    Dir.glob(File.join(api, "**/*.md")).each do |path|
      relative = path.delete_prefix(api + "/")
      parts = relative.split("/")
      title = File.basename(path, ".md")
      metadata = { "title" => title, "gh_edit_link" => false }
      body = File.read(path, encoding: "UTF-8")
      # The assembly index is published at /api/; update generated backlinks to it.
      body.gsub!(%r{(\]\((?:\.\./)+)DBase\.md(?=[)#])}, '\1index.md')
      if parts.length == 1
        metadata.merge!("title" => "API reference", "nav_order" => 8, "has_children" => true)
        body.sub!("# DBase assembly", "# API reference\n\nGenerated from the public API and XML comments on `main`. A published package may expose fewer APIs.")
      elsif parts.length == 2
        metadata.merge!("parent" => "API reference", "has_children" => File.directory?(path.delete_suffix(".md")))
      else
        metadata.merge!("parent" => parts[-2], "grand_parent" => "API reference")
      end
      File.write(path, metadata.to_yaml + "---\n\n" + body)
    end

    command = [Gem.ruby, Gem.bin_path("jekyll", "jekyll"), serve ? "serve" : "build",
               "--source", source, "--destination", File.join(artifacts, "docs-site"), "--strict_front_matter"]
    run!(*command)
  end
end
