# Deploying CrateWatch on Proxmox

Stack: `docker-compose.prod.yml` (postgres, C# backend, nginx frontend). Only the frontend is published, on port **8081**.
NPMplus terminates HTTPS for `cdwatcher.verkerkdigital.nl` and proxies to `<vm-ip>:8081`.

```
browser -> NPMplus (https) -> VM:8081 nginx -> /api/* -> backend:8080 -> postgres
                                            -> everything else: the static site
```

## 1. VM
1. Create a Debian 12 / Ubuntu 24.04 VM (2 vCPU, 2-4 GB RAM, 20-30 GB disk) with a fixed LAN IP, on the same network as NPMplus.
2. Install Docker Engine + the compose plugin (https://docs.docker.com/engine/install/).

## 2. Deploy
```bash
git clone <your private remote> cratewatch && cd cratewatch
cat > .env <<ENV
DB_PASSWORD=$(openssl rand -hex 16)
JWT_GENERATOR_SIGNATURE_SECRET=$(openssl rand -hex 32)
ENV
docker compose -f docker-compose.prod.yml up -d --build
```
Check `http://<vm-ip>:8081` from your LAN: you should see the login page. The first start applies the database migration.

## 3. First account (there is no registration)
```bash
docker compose -f docker-compose.prod.yml exec backend dotnet cratewatcher.dll create-user tom
```
It asks for a password (min 8 characters). Run it again for every extra user.

## 4. DNS and NPMplus
1. DNS: A record `cdwatcher.verkerkdigital.nl` -> your public IP (skip if a wildcard/DDNS record already covers it).
2. NPMplus -> Proxy Hosts -> Add:
   - Domain: `cdwatcher.verkerkdigital.nl`
   - Scheme `http`, Forward host `<vm-ip>`, port `8081`
   - Block common exploits: on. No access list needed (the app has its own login).
   - SSL tab: request a Let's Encrypt certificate, Force SSL, HTTP/2. Turn on HSTS after you have confirmed HTTPS works.

The app trusts exactly two `X-Forwarded-For` hops (NPMplus, then the frontend's nginx) to see the real client IP for the login rate limit. Do not add another proxy in between without changing `ForwardLimit` in `Program.cs`.
Limit port 8081 to the NPMplus IP with the **Proxmox VM firewall** (VM -> Firewall: input policy DROP, allow NPMplus -> 8081 and your LAN -> 22). Do not rely on `ufw`: Docker's published ports bypass it. Anyone who can reach 8081 directly can fake their IP and dodge the login rate limit.

## 5. Check
- https://cdwatcher.verkerkdigital.nl loads and you can log in.
- `curl -X POST https://cdwatcher.verkerkdigital.nl/api/v1/auth/register` returns 404.
- Six wrong passwords within a minute get "Too many attempts".

## 6. Backups and updates
- Nightly dump (cron), copied off the VM: `docker compose -f docker-compose.prod.yml exec -T postgres pg_dump -U cdwatcher cdwatcher | gzip > /backups/cratewatch-$(date +%F).sql.gz`
- Also schedule a Proxmox VM backup. Cover and photo caches can be rebuilt and need no backup.
- Update: snapshot the VM, then `git pull && docker compose -f docker-compose.prod.yml up -d --build`. Migrations run on start.
- Restore: `gunzip -c dump.sql.gz | docker compose -f docker-compose.prod.yml exec -T postgres psql -U cdwatcher cdwatcher` into an empty database.
