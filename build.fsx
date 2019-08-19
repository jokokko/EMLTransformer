#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet.Restore
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let version = "1.0.0"
let buildArtifactPath = "./artifacts"
let configuration = DotNet.BuildConfiguration.fromString "Release"
let fileVersion = (Environment.environVarOrDefault "APPVEYOR_BUILD_VERSION" (version + "." + "0"))

let versionArgs = [ @"/p:Version=""" + version + @""""; @"/p:AssemblyVersion=""" + fileVersion + @""""; @"/p:FileVersion=""" + fileVersion + @""""; @"/p:InformationalVersion=""" + fileVersion + @"""" ]

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ buildArtifactPath
    |> Shell.cleanDirs 
)

Target.create "Restore" (fun _ ->
    !! "src/**/*.csproj"    
    |> Seq.iter (DotNet.restore id)
)

Target.create "Build" (fun _ ->
  let setParams (defaults:DotNet.BuildOptions) =
        { defaults with
            NoRestore = true
            Configuration = configuration
        }
  let build = DotNet.build setParams
  !! "src/*.sln" |> Seq.iter build
)

Target.create "Test" (fun _ ->
  let setParams (defaults:DotNet.TestOptions) =
        { defaults with
            NoRestore = true
            Configuration = configuration
        }
  let test = DotNet.test setParams        
  !! "src/**/*.Tests.csproj"
  |> Seq.iter test
)

Target.create "Package" (fun _ ->       
    let props = versionArgs @ [ "--include-symbols"; ]
    let setParams (c:DotNet.PackOptions) = { c with 
      Configuration = configuration; 
      OutputPath = Some buildArtifactPath 
      NoBuild = true
      NoRestore = true
      Common = 
        DotNet.Options.Create()
        |> DotNet.Options.withCustomParams (Some (props |> String.concat " "))
    }
    let package = DotNet.pack setParams
    package "src/EMLTransformer/EMLTransformer.csproj"
)

Target.create "All" ignore

"Clean"
  ==> "Restore"
  ==> "Build"  
  ==> "Test"  
  ==> "Package"
  ==> "All"

Target.runOrDefault "All"
