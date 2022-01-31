#!/usr/bin/env sh

# abort on errors
set -e

cd Assets/SpriteDicing

git init
git add -A
git commit -m 'publish'
git push -f git@github.com:Elringus/SpriteDicing.git master:package

cd -
