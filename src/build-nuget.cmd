mkdir NuGet\lib\net40

copy bin\release\StaticWebHelper.dll NuGet\lib\net40
copy bin\release\WebMarkupMin.Core.dll NuGet\lib\net40

nuget pack NuGet\staticwebhelper.nuspec
