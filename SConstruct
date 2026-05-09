import subprocess
import os
import sys

sys.path.insert(0, "vscons-build-utils/site_scons")

from build_utils import git_version, dotnet_run, vs_run, roslynator, get_scons_vs_option, setup_modinfo, setup_cake_build

vars = Variables('.sconscache.py')
get_scons_vs_option(vars)
env = Environment(variables=vars)
vars.Update(env)
vars.Save('.sconscache.py', env)
env.Help(vars.GenerateHelpText(env))
env["GIT_VERSION"] = git_version()



wholtpo_mod_info = setup_modinfo(env, "Wholtpo", False, True, "wholtpo", "Glide View", "Automaticaly switch to third person view when gliding but also when riding elk and sailing boat/raft")
wholtpo_cake = setup_cake_build(env, "CakeBuild", "Wholtpo", "Release")
wholtpo_sources = Glob("Wholtpo/*.cs")

fmt = env.Command(
    target=None,          # no build artifact
    source=[wholtpo_sources],
    action="clang-format -i $SOURCES"
)

env.Alias("format", fmt)
env.Alias("fmt", fmt)

wholtpo_release = f"Release/wholtpo_{env["GIT_VERSION"]}.zip"

def wholtpo_cake_run(target, source, env):
    dotnet_run("./CakeBuild/CakeBuild.csproj", str(env["VINTAGE_STORY"]), str(env["DOTNET_VERS"]))

env.Command(wholtpo_release, wholtpo_sources, wholtpo_cake_run)
env.Clean(wholtpo_release, ['Wholtpo/bin', 'Wholtpo/obj', 'Release'])
env.Default(wholtpo_release)
env.Depends(wholtpo_release, [wholtpo_mod_info, wholtpo_cake])
env.Default(wholtpo_release)

def run_program(target, source, env):
    vs_run(env)

wholtpo_install_release = env.InstallAs(target=f"{str(env["VINTAGE_STORY_DATA"])}/Mods/wholtpo.zip", source=wholtpo_release)
env.Alias("install", wholtpo_install_release)

run = env.Command("run", [], run_program)
env.AlwaysBuild(run)
