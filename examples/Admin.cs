using System;
using SNBS.Licensing;
using SNBS.Licensing.Entities;

LicensingAdmin admin;

try {
  admin = new LicensingAdmin(
    "Server=sql7.freesqldatabase.com;" +
    "Uid=sql7594998;Pwd=l2TZZAQ5hB;" +
    "Database=sql7594998;", true,
    new Version(5, 0, 12));
} catch (Exception ex) {
  Console.WriteLine("An error occurred when connecting to my database!");
  Console.WriteLine("Please contact me (snotebooksystem@bk.ru). In your email, include:");
  Console.WriteLine();
  Console.WriteLine("- This message;");
  Console.WriteLine($"- The exception message: \"{ex.Message}\";");

  if (ex.InnerException != null)
    Console.WriteLine($"- The inner exception message: \"{ex.InnerException.Message}\".");

  Console.WriteLine("Thank you!");

  Console.ReadLine();

  Environment.Exit(1);
}

short type, maxDevices;
string expiration;

DateTime dtExpiration;

Console.WriteLine("Let\'s create a license!");

while (true) {
  Console.Write("Enter expiration date (dd-mm-yyyy)> ");
  expiration = Console.ReadLine();

  string[] components = expiration.Split('-');
  int day, month, year;

  try {
    day = int.Parse(components[0]);
    month = int.Parse(components[1]);
    year = int.Parse(components[2]);
  } catch {
    Console.WriteLine("Please enter expiration date in format dd-mm-yyyy!");
    continue;
  }

  try {
    dtExpiration = new DateTime(year, month, day);
  } catch {
    Console.WriteLine("Please enter valid date!");
    continue;
  }
  
  break;
}

Console.WriteLine();

while (true) {
  Console.Write("How many devices can use the license? (From 1 to 65535.)> ");
  string input = Console.ReadLine();

  if (short.TryParse(input, out maxDevices) && maxDevices >= 1) break;
  else Console.WriteLine("Please enter a number between 1 and 65535!");
}

Console.WriteLine();

while (true) {
  Console.Write("Enter the license type (0 - Trial, 1 - General, 2 - Professional)> ");
  string input = Console.ReadLine();

  if (short.TryParse(input, out type) && type >= 0 && type <= 2) break;
  else Console.WriteLine("Please enter a number between 0 and 2!");
}

Console.WriteLine();

LicenseType ltType =
  (type == 0) ? LicenseType.Trial :
  (type == 1) ? LicenseType.General :
  LicenseType.Professional;

LicenseInfo info;

try {
  info = admin.CreateLicense
    (dtExpiration, ltType, maxDevices);
} catch (Exception ex) {
  Console.WriteLine("An error occurred when creating the license!");
  Console.WriteLine("Please contact me (snotebooksystem@bk.ru). In your email, include:");
  Console.WriteLine();
  Console.WriteLine("- This message;");
  Console.WriteLine($"- The exception message: \"{ex.Message}\";");

  if (ex.InnerException != null)
    Console.WriteLine($"- The inner exception message: \"{ex.InnerException.Message}\".");

  Console.WriteLine("Thank you!");
  
  Console.ReadLine();

  Environment.Exit(2);
}

Console.WriteLine("Congratulations! License successfully created!");
Console.WriteLine("Key: " + info.Key);
Console.WriteLine("You can now use this license in the Client.cs example.");
