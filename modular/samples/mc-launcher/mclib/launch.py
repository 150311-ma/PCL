# -*- coding: utf-8 -*-
"""拼启动命令并拉起来。Java 找不对就用大白话说清楚，不让用户猜。"""
import os
import re
import subprocess

import meta


def find_java(preferred=None):
    candidates = []
    if preferred:
        candidates.append(preferred)

    home = os.environ.get("JAVA_HOME")
    if home:
        candidates.append(os.path.join(home, "bin", "java.exe" if os.name == "nt" else "java"))

    path = os.environ.get("PATH", "")
    for folder in path.split(";" if os.name == "nt" else ":"):
        if not folder.strip():
            continue
        candidate = os.path.join(folder.strip('"'), "java.exe" if os.name == "nt" else "java")
        if os.path.isfile(candidate):
            candidates.append(candidate)

    for candidate in candidates:
        if candidate and os.path.isfile(candidate):
            return candidate
    return None


def java_major(java_path):
    try:
        output = subprocess.run([java_path, "-version"], capture_output=True, text=True, timeout=20)
    except Exception:  # noqa: BLE001
        return 0

    text = (output.stderr or "") + (output.stdout or "")
    match = re.search(r'version "(\d+)(?:\.(\d+))?', text)
    if not match:
        return 0

    major = int(match.group(1))
    if major == 1 and match.group(2):     # 1.8.0_xxx 这种老写法
        return int(match.group(2))
    return major


def build_command(java_path, version_json, version_id, paths, version_dir, account,
                  memory_mb=2048, game_dir=None, extra_jvm=None):
    game_dir = game_dir or paths["root"]
    natives = os.path.join(version_dir, "natives")
    classpath = os.pathsep.join(meta.classpath_entries(
        version_json, paths["libraries"], os.path.join(version_dir, version_id + ".jar")))

    # 1.13 之后才有 arguments；更老的用 minecraftArguments，meta 里已经兼容
    values = {
        "auth_player_name": account["name"],
        "version_name": version_id,
        "game_directory": game_dir,
        "assets_root": paths["assets"],
        "assets_index_name": (version_json.get("assetIndex") or {}).get("id", version_id),
        "auth_uuid": account["uuid"].replace("-", ""),
        "auth_access_token": account["access_token"],
        "auth_session": account["access_token"],
        "user_type": account["user_type"],
        "version_type": version_json.get("type", "release"),
        "natives_directory": natives,
        "launcher_name": "PCV",
        "launcher_version": "1.0.0",
        "classpath": classpath,
        "classpath_separator": os.pathsep,
        "library_directory": paths["libraries"],
        "auth_xuid": account.get("xuid", ""),
        "clientid": "",
        "user_properties": "{}",
    }

    command = [java_path]
    command += ["-Xmx%dm" % memory_mb, "-Xms%dm" % min(512, memory_mb)]
    command += (extra_jvm or [])

    logging = (version_json.get("logging") or {}).get("client")
    if logging:
        log_file = os.path.join(paths["assets"], "log_configs", os.path.basename(logging["file"]["id"]))
        if os.path.isfile(log_file):
            command.append("-Dlog4j.configurationFile=" + log_file)

    command += meta.jvm_arguments(version_json, values)
    if "-cp" not in command and "-classpath" not in command:
        command += ["-cp", classpath]

    command.append(version_json["mainClass"])
    command += meta.game_arguments(version_json, values)
    return command


def launch(command, game_dir, say):
    say("→ 启动命令（截断显示）：")
    joined = " ".join('"%s"' % part if " " in part else part for part in command)
    say("  " + (joined[:900] + ("…" if len(joined) > 900 else "")))

    process = subprocess.Popen(command, cwd=game_dir,
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                               text=True, encoding="utf-8", errors="replace",
                               bufsize=1)
    return process
