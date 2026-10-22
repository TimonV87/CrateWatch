# Deploying CrateWatch on Proxmox (runbook)

Target: `https://cdwatcher.verkerkdigital.nl`. Stack: `docker-compose.prod.yml` (postgres, C# backend, nginx frontend).
Only the frontend is published, on port **8081**. NPMplus terminates HTTPS and proxies to `192.168.50.109:8081`.

```
browser -> NPMplus (https) -> VM 192.168.50.109:8081 nginx -> /api/* -> backend:8080 -> postgres
                                                           -> everything else: the static site
```

Placeholders: `<user>` = the user you made in the Ubuntu installer, `120` = your VM id in Proxmox.
Tick the boxes as you go.

## Progress

- [x] VM created, Ubuntu Server installed, boots (the fsck problem is gone after reinstall)
- [ ] 1. Code pushed from the Mac
- [x] 2. VM reachable over the network (the Mac's terminal app needed the macOS Local Network permission, see the note under "Fix: VM not reachable")
- [ ] 3. Docker installed
- [ ] 4. Code cloned on the VM
- [ ] 5. App started
- [ ] 6. First account created
- [ ] 7. DNS record + NPMplus proxy host
- [x] 8. Firewall on port 8081 (tested: direct access from the Mac times out)
- [ ] 9. Verified from outside
- [ ] 10. Backups

---

## Fix: VM not reachable (current problem)
docker compose -f docker-compose.prod.yml exec backend dotnet cratewatcher.dll create-user Thijmen
Symptom: `ssh <user>@192.168.50.109` gives `No route to host`.
What we found from the Mac: nothing answers at `192.168.50.109` (ARP entry "incomplete"), the router `192.168.50.1` works, and ports 22 are closed on `.103`, `.203`, `.204`, `.200`. So the VM is not on `.109` and not reachable by SSH on any of the other `bc:24:11` (Proxmox VM) addresses either.

Work from the VM's own console, not from SSH:

1. Proxmox web UI -> your VM -> **Console**. Log in with the installer user. Check the VM is **started** (green icon).
2. In the console:
   ```bash
   ip a                         # which interface exists (ens18?) and does it have an inet address?
   ip route                     # is there a "default via 192.168.50.1"?
   ls /etc/netplan/             # note the file name
   sudo cat /etc/netplan/*.yaml
   ```
3. Interpret:
   - **No `inet 192.168.50.x` address, or the interface is `DOWN`:** fix the netplan file below, then `sudo netplan apply`.
   - **A different address (e.g. `192.168.50.2xx`):** the static setting was not applied. Use that address for now, or fix netplan.
   - **Correct address but still unreachable from the Mac:** in Proxmox check VM -> **Hardware** -> Network Device: bridge `vmbr0`, and "Firewall" **unchecked** for now. Also check `VM -> Firewall -> Options` is not set to DROP.
   - **No `ens18`/interface at all:** Hardware -> add a Network Device (VirtIO, `vmbr0`), reboot.
4. Netplan file (`sudo nano /etc/netplan/50-cloud-init.yaml`, or whichever file exists). Replace `ens18` with your interface name from `ip a`:
   ```yaml
   network:
     version: 2
     ethernets:
       ens18:
         addresses: [192.168.50.109/24]
         routes:
           - to: default
             via: 192.168.50.1
         nameservers:
           addresses: [192.168.50.1]
   ```
   Note: in this file the address is `192.168.50.109/24` (host address with prefix). The installer's separate "Subnet" field used `192.168.50.0/24` (the network).
   ```bash
   sudo netplan apply
   ip a
   ping -c2 192.168.50.1
   ping -c2 1.1.1.1
   ```
5. Install and start SSH if it is missing (a "connection refused" error means SSH is not running, "No route to host" means the network is the problem):
   ```bash
   sudo apt update && sudo apt -y install openssh-server qemu-guest-agent
   sudo systemctl enable --now ssh
   ```
6. Back on the Mac:
   ```bash
   ping -c2 192.168.50.109
   ssh <user>@192.168.50.109
   ```

Note (what it turned out to be): macOS 26 blocks terminal apps from reaching LAN devices unless they have **System Settings -> Privacy & Security -> Local Network** enabled. Symptom: "No route to host" for every LAN device except the router, while the browser still works. Fix: enable the toggle for your terminal app and restart it.

Optional: in your router, reserve `192.168.50.109` for the VM's MAC address so it never changes. Your router is `192.168.50.1`.

---

## 1. Mac: push the code

```bash
cd ~/workspace/cd-watcher
git status                      # review what will be committed
git add -A
git reset .idea                 # leave IDE files out (optional)
git commit -m "Production deploy: C# backend, no registration, nginx frontend"
git push origin master
```

`.env` is gitignored, so no secrets are pushed.

## 3. VM: install Docker

```bash
ssh <user>@192.168.50.109

sudo apt update && sudo apt -y upgrade
sudo apt -y install git curl
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
exit                              # log out and back in so the group applies
ssh <user>@192.168.50.109
docker run --rm hello-world       # should print "Hello from Docker!"
```

Optional, so logs cannot fill the disk:

```bash
echo '{"log-driver":"json-file","log-opts":{"max-size":"10m","max-file":"3"}}' | sudo tee /etc/docker/daemon.json
sudo systemctl restart docker
```

## 4. VM: get the code

The repo is private (`git@github.com:TimonV87/CrateWatch.git`), so give the VM a read-only key.

```bash
ssh-keygen -t ed25519 -C "cratewatch-vm" -f ~/.ssh/id_ed25519 -N ""
cat ~/.ssh/id_ed25519.pub
```

On GitHub: repo -> Settings -> Deploy keys -> Add deploy key. Paste the key, leave "Allow write access" **off**.

```bash
ssh -T git@github.com             # type "yes" for the host prompt
git clone git@github.com:TimonV87/CrateWatch.git cratewatch
cd cratewatch
```

## 5. VM: start the app

```bash
cat > .env <<ENV
DB_PASSWORD=$(openssl rand -hex 16)
JWT_GENERATOR_SIGNATURE_SECRET=$(openssl rand -hex 32)
ENV
chmod 600 .env

docker compose -f docker-compose.prod.yml up -d --build   # first build takes a few minutes
docker compose -f docker-compose.prod.yml ps               # 3 services "Up", postgres "healthy"
docker compose -f docker-compose.prod.yml logs backend     # look for "Applied migration"
curl -I http://localhost:8081                              # HTTP/1.1 200 OK
```

Right after `up`, `/api` may answer 502 for a few seconds while the backend finishes starting.

## 6. VM: first account (there is no registration page)

```bash
docker compose -f docker-compose.prod.yml exec backend dotnet cratewatcher.dll create-user tom
```

Prompts:

```
Password for tom (min 8 characters):      <type it, nothing shows>
Repeat password:                          <type it again>
User 'tom' created.
```

Run it again for each extra user. Do not add `-T`: the hidden prompt needs a terminal.
From your Mac open `http://192.168.50.109:8081` and log in.

## 7. DNS and NPMplus

1. DNS: at the DNS provider for `verkerkdigital.nl` add an **A record** `cdwatcher` -> your public IP (skip if a wildcard/DDNS record already covers it).
2. NPMplus web UI -> Proxy Hosts -> Add Proxy Host:
   - **Details:** Domain `cdwatcher.verkerkdigital.nl`, Scheme `http`, Forward Hostname/IP `192.168.50.109`, Port `8081`, Block Common Exploits **on**, no Access List.
   - **SSL:** Request a new certificate (Let's Encrypt), turn on Force SSL and HTTP/2, accept the terms, Save.
   - If the certificate fails: check DNS has propagated (`dig cdwatcher.verkerkdigital.nl`) and that ports 80 and 443 on the router reach NPMplus.
3. After HTTPS works: edit the proxy host again and turn on HSTS.

## 8. Lock down port 8081 (Proxmox firewall, not ufw)

Docker's published ports bypass `ufw`, so use the Proxmox VM firewall. Proxmox web UI:

- Three switches must all be on, or no rule applies: (1) Datacenter -> Firewall -> Options -> Firewall = yes (`pve-firewall status` must say `enabled/running`), (2) VM -> Firewall -> Options -> Firewall = yes, (3) VM -> Hardware -> Network Device -> Firewall ticked.
- VM -> Firewall -> Options: **Firewall = yes**, **Input Policy = DROP**.
- VM -> Firewall -> add rules: `IN ACCEPT` tcp, source `<NPMPLUS-IP>`, dest port `8081`; and `IN ACCEPT` tcp, source `192.168.50.0/24`, dest port `22`.
- Datacenter -> Firewall -> Options: must also be **enabled** for VM rules to apply. Before enabling it, make sure you can still reach the Proxmox GUI (port 8006) from your Mac.

Test from a machine other than NPMplus (it should time out): `curl -m 3 http://192.168.50.109:8081`

The app trusts exactly two `X-Forwarded-For` hops (NPMplus + the frontend's nginx) to see the real client IP for the login rate limit. Do not add another proxy in between without changing `ForwardLimit` in `Program.cs`.

## 9. Verify from outside

```bash
curl -sI https://cdwatcher.verkerkdigital.nl | head -3                                   # HTTP/2 200
curl -s -o /dev/null -w '%{http_code}\n' -X POST https://cdwatcher.verkerkdigital.nl/api/v1/auth/register   # 404
for i in 1 2 3 4 5 6; do curl -s -o /dev/null -w '%{http_code} ' -X POST https://cdwatcher.verkerkdigital.nl/api/v1/login -H 'Content-Type: application/json' -d '{"username":"x","password":"wrongwrong"}'; done; echo
# expected: 401 401 401 401 401 429
```

Then in a browser: log in, add an artist, flip a switch, mark an album.

## 10. Backups

```bash
sudo mkdir -p /var/backups/cratewatch && sudo chown $USER /var/backups/cratewatch
cd ~/cratewatch
docker compose -f docker-compose.prod.yml exec -T postgres pg_dump -U cdwatcher cdwatcher | gzip > /var/backups/cratewatch/test.sql.gz
ls -lh /var/backups/cratewatch/       # test one dump by hand first

crontab -e
```

Add this one line (the `\%` is required in cron):

```
15 3 * * * cd $HOME/cratewatch && docker compose -f docker-compose.prod.yml exec -T postgres pg_dump -U cdwatcher cdwatcher | gzip > /var/backups/cratewatch/cratewatch-$(date +\%F).sql.gz && find /var/backups/cratewatch -name '*.sql.gz' -mtime +14 -delete
```

Also in Proxmox: Datacenter -> Backup -> Add: VM 120, mode Snapshot, nightly, keep ~7. Copy the dumps off the VM too (another machine or disk).

Restore into an empty database:

```bash
gunzip -c dump.sql.gz | docker compose -f docker-compose.prod.yml exec -T postgres psql -U cdwatcher cdwatcher
```

## Updating later

```bash
# Proxmox host, optional safety net:
qm snapshot 120 pre-update

# on the VM:
cd ~/cratewatch
git pull
docker compose -f docker-compose.prod.yml up -d --build
docker image prune -f
```

Migrations run by themselves when the backend starts.

## Handy commands

```bash
docker compose -f docker-compose.prod.yml ps                  # what is running
docker compose -f docker-compose.prod.yml logs -f backend     # live backend log (Ctrl+C to leave)
docker compose -f docker-compose.prod.yml restart backend
docker compose -f docker-compose.prod.yml down                # stop everything, data volumes stay
docker compose -f docker-compose.prod.yml up -d               # start again without rebuilding
```

## Troubleshooting

- **NPMplus shows 502:** it cannot reach the VM. From the NPMplus machine run `curl -I http://192.168.50.109:8081`. If that fails, check the Proxmox firewall rule (step 8) and `docker compose ps`.
- **Certificate request fails:** DNS has not propagated, or ports 80/443 do not reach NPMplus.
- **Login says "Too many attempts":** wait a minute. The limit is 5 tries per minute per IP.
- **502 for a few seconds after an update:** the backend is still starting.
- **"Could not add artist":** search for the artist first (the app only accepts artists from a recent search). If it still fails, MusicBrainz is probably rate-limiting; wait a minute.
- **Forgot a password:** there is no reset command yet, and `create-user` refuses an existing name. Deleting the user row would also delete their lists, so ask for a `reset-password` command first.
