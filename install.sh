#!/bin/sh

# Install nodejs
apt update
apt install nodejs -y
apt install npm -y
npm install node-cron --save

# Install dotnet 5

# Install AWS

# Build/Deploy the tools

echo "Installation Complete"
echo "Confirm the constants at the top of app.js"
echo "Use 'node app.js' and connect with a browser to manage the configuration"
