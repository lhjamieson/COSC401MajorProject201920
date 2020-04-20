Another column has been added to the database so you will need to reimport the CreateDatabase file as explained below: 

In MySQL workbench, click the administration tab on the left

Go to data import/restore

Select "import from self contained file" and use CreateDatabase.sql from the repo (this is NOT the same as the old one, make sure youre using the new one from master)

make sure the default target schema is greenwelldatabase

click start import at the bottom

Now, open the solution in VS

click tools -> nuget package manager -> package manager console

in the console, run the command "add-migration migrationName -context ApplicationDbContext"

When that is finished, run "update-database -context ApplicationDbContext"

make sure appsettings.json has the password for your MySQL server and you should be good to go



IMPORTANT NOTE ABOUT ACCOUNTS:

This commit also adds two default users of both Admin and Member roles because the process of creating new users is difficult because our email sending functionality is broken.
 
Default User:
Email: default@test.com
Pwd: Password_123

Admin User:
Email: admin@test.com
Pwd: Password_123

IMPORTANT NOTE ABOUT UPLOADS:

For more information see the github merge comment here:

A major merge that adds the requirement of admin approval to default user's upgrades with the option to approve/reject and download the file on the admin portal.  It also fixes a lot of hidden bugs in the file-system that struggled with subfolders and subfolders with lots of siblings.  Also, a check was added for duplicate file uploads as that was a major source of bugs. Additionally, we switched from the old onClick function that detected the file/folder the user clicked to the native react-keyed-file-browser onFolderSelected and onFileSelected which better identifies which file/folder was selected. 

It requires a new column in the database for whether a file is approved, so it won't run until you either add another column named, "approved" or use the CreateDatabase.sql file to recreate the database. It's also important to note that any files that a member uploads will be invisible until an admin approves them so don't be surprised when you upload a file as a member and it doesn't appear.

It also adds two default users of both Admin and Member roles because the process of creating new users is difficult because our email sending functionality is broken.
 




