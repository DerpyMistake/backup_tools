#!/bin/sh

dotnet publish BackupTools/BackupTools.sln -p:PublishProfile=ToolsFolder --force
