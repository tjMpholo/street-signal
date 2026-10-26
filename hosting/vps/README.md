# VPS deployment plan

Domain: street-signal.duetonetechsolutions.com. SSH alias: xneelo.

These are templates, not an executed deployment. Port 5287 must be confirmed unused before installation. Existing Nginx sites and services must be inspected first; never overwrite the default site or stop another application.

- Inspect architecture, distribution, listeners, Nginx includes and certificates.
- Publish self-contained for the confirmed Linux architecture, avoiding changes to the other application's .NET installation.
- Install under /opt/street-signal/releases with current symlink; use dedicated service account streetsignal.
- Store Firebase credentials in /etc/street-signal with root-owned directory and service-group read permissions. Set GOOGLE_APPLICATION_CREDENTIALS and Firebase__ApiKey through a protected environment file. Do not put keys under the web root.
- Persist the data-protection key ring under /var/lib/street-signal/keys with 0700 permissions.
- Install a separate street-signal systemd unit and Nginx server block for this exact hostname only.
- Verify DNS points to this server. Obtain a certificate using the isolated webroot challenge. Enable TLS and verify `nginx -t` before reloading Nginx. Never restart/replace existing sites as part of deployment.
- Check localhost health, HTTPS login, response headers and authorized sign-in. Confirm existing app health before and after.
- Roll back by restoring only Street Signal's previous release/configuration.

The application trusts forwarded protocol/IP headers only from loopback proxies. The Nginx site must overwrite X-Forwarded-For with $remote_addr rather than accepting visitor headers. HTTPS is required for production session cookies.

Deployment status: application installed on xneelo (154.65.111.33), dedicated systemd unit street-signal.service, localhost port 5287, release /opt/street-signal/releases/20261026-01. Firebase credentials installed under /etc/street-signal after explicit user authorization. Backend /health returns 200. Existing cosmosplay.service retains PID 3713160 and its Nginx configuration SHA256 766f9d6e6845ce3c5123c6f561715103b9893170c44d89aa0318c0c514edf2af. The existing public app returns 200. New subdomain is currently an HTTP maintenance endpoint until DNS and certificate issuance complete. Required DNS: A record street-signal -> 154.65.111.33. TLS template is prepared separately and must not be enabled before certificate issuance.

HTTPS deployment complete: certificate issued for street-signal.duetonetechsolutions.com, expires 2027-01-24, renewal scheduled through certbot.timer with a validated Nginx reload deploy hook. New TLS site enabled. Login returns 200, /health returns status ok, HTTP redirects 301 to HTTPS, and external HTTPS check against 154.65.111.33 passes. Existing app still returns 200 with unchanged process PID and Nginx configuration hash. No authenticated production sign-in was performed by the agent; the user must verify using their own password.
