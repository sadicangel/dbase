# Documentation

The site uses Jekyll and Just the Docs, matching the documentation stack in `avro-source-generator`. Its default URL is https://sadicangel.github.io/dbase/. Pages describe `main`; there are no separately archived release docs yet.

## Local build

With Ruby 3.4 and Bundler installed, run from the repository root:

```shell
bundle install --gemfile docs/Gemfile
bundle exec --gemfile docs/Gemfile jekyll build --source docs --destination artifacts/docs-site --strict_front_matter
bundle exec --gemfile docs/Gemfile ruby docs/check-links.rb artifacts/docs-site /dbase
dotnet run --project docs/examples/DBase.Docs.Examples.csproj --configuration Release
```

For a local preview:

```shell
bundle exec --gemfile docs/Gemfile jekyll serve --source docs --destination artifacts/docs-site
```

Open http://localhost:4000/dbase/. Build output is ignored. Commit `Gemfile.lock` when changing dependencies. Markdown links between documentation pages are rewritten by Jekyll to their published URLs.

## GitHub Pages setup

In the repository, select **Settings > Pages > Build and deployment > Source > GitHub Actions**. The workflow uses the built-in `GITHUB_TOKEN` with Pages/OIDC permissions; no deployment secret is needed. Ensure the `github-pages` environment permits deployments from `main` if environment restrictions are configured.

After the files reach `main`, the docs workflow builds the site, checks local links, runs the C# examples, and deploys. Pull requests build and validate without deploying. A manual workflow dispatch on `main` can rebuild the site; dispatching another branch only builds it. Source changes also trigger checks because they can invalidate the examples.

The workflow does not enable Pages or change repository settings automatically. If the repository is renamed or moved, update `url`, `baseurl`, and repository links in `_config.yml`.

The deployment follows the [GitHub Pages custom workflow requirements](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages).
