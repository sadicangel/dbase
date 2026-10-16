# Check generated HTML locally; external sites are not contacted.
require "nokogiri"
require "pathname"
require "set"
require "uri"

abort "Usage: ruby docs/check-links.rb DIRECTORY BASEURL" unless ARGV.length == 2
root = File.realpath(ARGV[0])
prefix = "/#{ARGV[1].gsub(%r{\A/+|/+\z}, '')}"
origin = "https://sadicangel.github.io"

pages = Dir.glob(File.join(root, "**/*.html")).sort.to_h do |path|
  document = Nokogiri::HTML(File.read(path, encoding: "UTF-8"))
  ids = document.css("[id], a[name]").flat_map { |node| [node["id"], node["name"]].compact }.to_set
  links = document.css("[href], [src]").flat_map { |node| [node["href"], node["src"]].compact }.reject(&:empty?)
  [File.realpath(path), { ids: ids, links: links }]
end
abort "No HTML pages found in #{root}" if pages.empty?

errors = []
pages.each do |path, page|
  relative = Pathname.new(path).relative_path_from(Pathname.new(root)).to_s.tr("\\", "/")
  page[:links].each do |link|
    label = "#{relative}: #{link}"
    begin
      url = URI.join("#{origin}#{prefix}/#{relative}", link)
      next unless %w[http https].include?(url.scheme) && url.host == URI(origin).host

      decoded = URI::DEFAULT_PARSER.unescape(url.path)
      unless decoded == prefix || decoded.start_with?(prefix + "/")
        errors << "#{label}: missing base path #{prefix}"
        next
      end
      target = File.expand_path(decoded.delete_prefix(prefix).sub(%r{\A/+}, ""), root)
      target = File.realpath(target) if File.exist?(target)
      unless target == root || target.start_with?(root + File::SEPARATOR)
        errors << "#{label}: path escapes site root"
        next
      end
      target = File.join(target, "index.html") if File.directory?(target)
      unless File.file?(target)
        errors << "#{label}: target does not exist"
        next
      end
      fragment = URI::DEFAULT_PARSER.unescape(url.fragment.to_s)
      if !fragment.empty? && File.extname(target) == ".html" && !pages.fetch(File.realpath(target))[:ids].include?(fragment)
        errors << "#{label}: fragment ##{fragment} does not exist"
      end
    rescue URI::InvalidURIError => error
      errors << "#{label}: invalid URL: #{error.message}"
    end
  end
end

abort errors.join("\n") unless errors.empty?
puts "Validated #{pages.length} HTML pages: all local links, fragments, and assets exist."
