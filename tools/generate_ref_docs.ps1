dotnet restore generate_ref_docs.sln -v minimal /m
dotnet build generate_ref_docs.sln --no-restore -c Release -v minimal /m

dotnet restore "../src/CommandLine.sln" -v minimal /m
dotnet build "../src/CommandLine.sln" --no-restore -c Release -v minimal /m

& "../src/CommandLine/bin/Release/net7.0/Roslynator" generate-doc generate_ref_docs.sln `
 --properties Configuration=Release `
 -o "build/ref" `
 --host docusaurus `
 --heading "Roslynator .NET API Reference" `
 --group-by-common-namespace `
 --ignored-common-parts content `
 --max-derived-types 10 `
 --root-file-path "build/ref.md"
