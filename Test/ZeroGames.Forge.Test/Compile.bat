@echo off

cd /d %~dp0
cd ..\..\Compiler\ZeroGames.Forge.Compiler.Client\bin\Debug\net9.0

set sourcedirs="../../../../../Test/ZeroGames.Forge.Test/"
set sources=main;shared
set outputdir="../../../../../Test/ZeroGames.Forge.Test/obj/Generated/"

ZeroGames.Forge.Compiler.Client.exe sourcedirs=%sourcedirs% sources=%sources% outputdir=%outputdir%

pause


