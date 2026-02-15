REG_PATH="HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\bc8a6440-918f-11dd-ad8b-0800200c9a66_is1"
VALUE_KEY="InstallLocation"
INSTALL_PATH=$(reg query "$REG_PATH" -v "$VALUE_KEY" 2>/dev/null | sed -n 's/.*REG_SZ\s*//p' | tr -d '\r')

# if this doesn't find your ddo installation, set it here
# INSTALL_PATH="c:\something\blah\blah\" # note, include trailing "\"

if [ -z "$INSTALL_PATH" ]; then
  echo "Could not find DDO installation folder. Open and edit this script to set it."
else
  echo "DDO installation found at: $INSTALL_PATH"
fi

echo "DDO_INSTALL_PATH=$INSTALL_PATH" > .env

docker build -t ddodatapi src/DdoDatApi/
docker compose up -d

