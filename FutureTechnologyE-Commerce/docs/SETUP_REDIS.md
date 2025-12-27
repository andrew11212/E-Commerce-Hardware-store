# Redis Setup Guide for FutureTechnology E-Commerce

## Overview
This guide will help you set up Redis for caching in the FutureTechnology E-Commerce application.

## Prerequisites
- Windows 10/11 or Windows Server
- Administrator access
- .NET 8.0 SDK

## Installation Options

### Option 1: Using Chocolatey (Recommended for Windows)

1. **Install Chocolatey** (if not already installed)
   - Open PowerShell as Administrator
   - Run:
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
   ```

2. **Install Redis**
   ```powershell
   choco install redis-64 -y
   ```

3. **Start Redis Service**
   ```powershell
   redis-server
   ```

### Option 2: Using Windows Subsystem for Linux (WSL)

1. **Install WSL** (if not already installed)
   ```powershell
   wsl --install
   ```

2. **Install Redis in WSL**
   ```bash
   sudo apt update
   sudo apt install redis-server -y
   ```

3. **Start Redis**
   ```bash
   sudo service redis-server start
   ```

4. **Configure Redis to accept connections from Windows**
   ```bash
   sudo nano /etc/redis/redis.conf
   # Change 'bind 127.0.0.1' to 'bind 0.0.0.0'
   # Save and restart Redis
   sudo service redis-server restart
   ```

### Option 3: Using Docker (Recommended for Development)

1. **Install Docker Desktop for Windows**
   - Download from: https://www.docker.com/products/docker-desktop

2. **Run Redis Container**
   ```powershell
   docker run -d --name redis-cache -p 6379:6379 redis:latest
   ```

3. **Verify Redis is Running**
   ```powershell
   docker ps
   ```

### Option 4: Manual Installation

1. **Download Redis for Windows**
   - Visit: https://github.com/microsoftarchive/redis/releases
   - Download the latest .msi installer

2. **Install Redis**
   - Run the installer
   - Follow the installation wizard
   - Check "Add to PATH" option

3. **Start Redis**
   ```powershell
   redis-server
   ```

## Verification

### Test Redis Connection

1. **Using Redis CLI**
   ```powershell
   redis-cli ping
   ```
   Expected output: `PONG`

2. **Test Set/Get Operations**
   ```powershell
   redis-cli
   > SET test "Hello Redis"
   > GET test
   > EXIT
   ```

3. **Using C# Code**
   Create a test file and run:
   ```csharp
   using StackExchange.Redis;
   
   var redis = ConnectionMultiplexer.Connect("localhost:6379");
   var db = redis.GetDatabase();
   
   db.StringSet("test", "Hello from C#");
   var value = db.StringGet("test");
   Console.WriteLine($"Value: {value}");
   ```

## Configuration

### Application Configuration

The application is already configured to use Redis. Verify `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false,connectTimeout=5000,syncTimeout=5000"
  }
}
```

### Redis Configuration (Optional)

For production, you may want to configure Redis with a password:

1. **Edit redis.conf**
   ```bash
   # Find and uncomment the requirepass line
   requirepass your_strong_password_here
   ```

2. **Update Connection String**
   ```json
   {
     "ConnectionStrings": {
       "Redis": "localhost:6379,password=your_strong_password_here,abortConnect=false"
     }
   }
   ```

## Running as Windows Service

### Using NSSM (Non-Sucking Service Manager)

1. **Install NSSM**
   ```powershell
   choco install nssm -y
   ```

2. **Create Redis Service**
   ```powershell
   nssm install Redis "C:\Program Files\Redis\redis-server.exe" "C:\Program Files\Redis\redis.windows.conf"
   ```

3. **Start Service**
   ```powershell
   nssm start Redis
   ```

## Monitoring Redis

### Redis CLI Commands

```bash
# Monitor all commands in real-time
redis-cli monitor

# Get server information
redis-cli info

# Check memory usage
redis-cli info memory

# View all keys
redis-cli keys *

# Get key count
redis-cli dbsize

# Check specific key
redis-cli get "FutureTech_home_index_data"

# Delete specific key
redis-cli del "FutureTech_home_index_data"

# Clear all cache (use with caution!)
redis-cli flushall
```

### Performance Monitoring

```bash
# Check latency
redis-cli --latency

# Check latency history
redis-cli --latency-history

# Monitor stats
redis-cli --stat
```

## Troubleshooting

### Issue: Redis not starting

**Solution 1**: Check if port 6379 is already in use
```powershell
netstat -ano | findstr :6379
```

**Solution 2**: Try a different port
```powershell
redis-server --port 6380
```
Update connection string accordingly.

### Issue: Connection timeout

**Solution**: Increase timeout in connection string
```json
"Redis": "localhost:6379,abortConnect=false,connectTimeout=10000,syncTimeout=10000"
```

### Issue: Out of memory

**Solution**: Configure maxmemory in redis.conf
```bash
maxmemory 256mb
maxmemory-policy allkeys-lru
```

### Issue: Application can't connect to Redis

**Solution**: Check firewall settings
```powershell
# Allow Redis through Windows Firewall
New-NetFirewallRule -DisplayName "Redis" -Direction Inbound -LocalPort 6379 -Protocol TCP -Action Allow
```

## Production Deployment

### Azure Redis Cache

1. **Create Azure Redis Cache**
   - Go to Azure Portal
   - Create new Redis Cache resource
   - Choose appropriate tier (Standard or Premium)

2. **Get Connection String**
   - Navigate to Access Keys
   - Copy Primary Connection String

3. **Update appsettings.json**
   ```json
   {
     "ConnectionStrings": {
       "Redis": "your-redis-name.redis.cache.windows.net:6380,password=your-access-key,ssl=True,abortConnect=False"
     }
   }
   ```

### AWS ElastiCache

1. **Create ElastiCache Cluster**
   - Choose Redis engine
   - Configure node type and cluster mode

2. **Get Endpoint**
   - Copy Primary Endpoint

3. **Update Connection String**
   ```json
   {
     "ConnectionStrings": {
       "Redis": "your-cluster.cache.amazonaws.com:6379,abortConnect=false"
     }
   }
   ```

## Performance Tuning

### Recommended Settings for Production

```conf
# redis.conf

# Maximum memory
maxmemory 2gb
maxmemory-policy allkeys-lru

# Persistence (choose one)
# Option 1: RDB (snapshots)
save 900 1
save 300 10
save 60 10000

# Option 2: AOF (append-only file)
appendonly yes
appendfsync everysec

# Network
tcp-backlog 511
timeout 0
tcp-keepalive 300

# Performance
maxclients 10000
```

### Connection Pooling

The application uses StackExchange.Redis which handles connection pooling automatically. The `IConnectionMultiplexer` is registered as a singleton.

## Backup and Recovery

### Manual Backup

```bash
# Save current state
redis-cli save

# Backup RDB file
cp /var/lib/redis/dump.rdb /backup/dump_$(date +%Y%m%d_%H%M%S).rdb
```

### Restore from Backup

```bash
# Stop Redis
sudo service redis-server stop

# Replace RDB file
cp /backup/dump_20250102_120000.rdb /var/lib/redis/dump.rdb

# Start Redis
sudo service redis-server start
```

## Security Best Practices

1. **Use Strong Password**
   ```conf
   requirepass ComplexPassword123!@#
   ```

2. **Bind to Specific IP**
   ```conf
   bind 127.0.0.1 192.168.1.100
   ```

3. **Disable Dangerous Commands**
   ```conf
   rename-command FLUSHDB ""
   rename-command FLUSHALL ""
   rename-command CONFIG ""
   ```

4. **Enable SSL/TLS** (for production)
   ```conf
   tls-port 6380
   tls-cert-file /path/to/redis.crt
   tls-key-file /path/to/redis.key
   ```

## Next Steps

1. Start Redis service
2. Run the application
3. Monitor cache hit rates in application logs
4. Adjust cache expiration times based on usage patterns
5. Set up monitoring and alerts

## Support

For issues or questions:
- Redis Documentation: https://redis.io/documentation
- StackExchange.Redis: https://stackexchange.github.io/StackExchange.Redis/
- Application Issues: Check application logs in `logs/` directory
