# Admin Account Seeding

## Overview
This application automatically seeds an admin account when it starts up for the first time. The seeding process creates necessary roles and an admin user with full permissions.

## Default Admin Credentials
- **Email**: `admin@futuretech.com`
- **Password**: `Admin@123456`
- **Role**: `Admin`

## Roles Created
The seeding process creates the following roles:
- `Admin` - Full system administration
- `Employee` - Employee access level
- `Customer` - Regular customer access
- `Company` - Company/B2B customer access

## Admin User Details
The seeded admin user includes:
- Full name: Admin User
- Phone: 01234567890
- Address: Admin Street, Building 1, A101, Floor 1, Cairo, Egypt
- Email confirmed: Yes
- Phone confirmed: Yes

## Security Notes
⚠️ **IMPORTANT**: Change the default admin password immediately after first login!

### Recommended Actions:
1. Log in with default credentials
2. Navigate to Profile/Account settings
3. Change password to a strong, unique one
4. Optionally update email and contact information

## How It Works
The seeding process runs automatically during application startup:
1. Applies any pending database migrations
2. Creates roles if they don't exist
3. Creates admin user if it doesn't exist
4. Assigns admin role to the user
5. Logs all operations for debugging

## Manual Seeding
If you need to reseed or create additional admin accounts, you can:
1. Delete the existing admin user from the database
2. Restart the application
3. The seeding process will create a new admin account

## Troubleshooting
Check the application logs for seeding-related messages:
- `logs/log-*.txt` files in the application directory
- Console output during startup

Common issues:
- **Database connection errors**: Ensure your connection string is correct
- **Permission errors**: Make sure the application has database write permissions
- **Existing admin**: seeding will skip if admin already exists

## Customization
To modify the admin seeding:
- Edit `Program.cs` methods: `SeedAdminUserAsync`, `SeedRolesAsync`
- Change default credentials in the admin user creation code
- Adjust role assignments as needed

## Production Deployment
For production environments:
1. Always change default admin password immediately
2. Consider using environment variables for admin credentials
3. Enable email confirmation for additional security
4. Implement password complexity requirements
