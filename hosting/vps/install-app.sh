#!/usr/bin/env bash
set -euo pipefail
stage=/home/ubuntu/.street-signal-upload
release=/opt/street-signal/releases/20261026-01
if test -e /opt/street-signal || test -e /etc/systemd/system/street-signal.service || test -e /etc/nginx/sites-available/street-signal; then
 echo 'Street Signal paths already exist; refusing to overwrite.' >&2; exit 1
fi
if ss -ltnH 'sport = :5287' | grep -q .; then echo 'Port 5287 is in use.' >&2; exit 1; fi
if id streetsignal >/dev/null 2>&1; then echo 'Service account already exists; review required.' >&2; exit 1; fi
useradd --system --user-group --home-dir /var/lib/street-signal --shell /usr/sbin/nologin streetsignal
install -d -m 0755 /opt/street-signal/releases
install -d -m 0755 "$release"
tar -xzf "$stage/street-signal-linux.tar.gz" -C "$release" --no-same-owner
chown -R root:root "$release"
chmod 0755 "$release/StreetSignal.Dashboard"
ln -s "$release" /opt/street-signal/current
install -d -o root -g streetsignal -m 0750 /etc/street-signal
install -d -o streetsignal -g streetsignal -m 0700 /var/lib/street-signal /var/lib/street-signal/keys
install -d -o root -g root -m 0755 /var/www/street-signal-acme/.well-known/acme-challenge
install -o root -g root -m 0644 "$stage/street-signal.service" /etc/systemd/system/street-signal.service
systemd-analyze verify /etc/systemd/system/street-signal.service
systemctl daemon-reload
# Service is intentionally not started until protected credentials are installed.
printf 'Application and isolated service installed; waiting for credentials and DNS.\n'
sha256sum /etc/nginx/sites-available/cosmosplay
systemctl show cosmosplay.service -p MainPID -p ActiveState
curl -s -o /dev/null -w 'existing_https_status=%{http_code}\n' https://duetonetechsolutions.co.za