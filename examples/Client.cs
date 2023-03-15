using System;

using SNBS.Licensing;

using SNBS.Licensing.Entities;

LicensingClient client;

try {

    client = new LicensingClient(

        "Server=sql7.freesqldatabase.com;" +

        "Uid=sql7594998;Pwd=l2TZZAQ5hB;" +

        "Database=sql7594998;",

        "Example", true,

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

while (true) {

    Console.Write("What will we do? 0 for viewing the current license, 1 for applying a license> ");

    string input = Console.ReadLine();

    switch (input) {

        default:

            Console.WriteLine("Enter 0 to view the current license or 1 to apply a new license!");

            continue;

        case "0":

            var info = client.GetCurrentLicense();

            

            Console.WriteLine();

            

            if (info.Usability == LicenseUsability.Usable) {

                Console.WriteLine($"Key: {info.Key}");

#pragma warning disable CS8604   // Suppresses a warning that hasn't got any value when you're using properties of `LicenseInfo` and you're sure the `Usability` property equals to `LicenseUsability.Usable`

                Console.WriteLine($"Expiration: {info.Expiration?.ToLongDateString()}");

#pragma warning restore CS8604

                Console.WriteLine($"Type: {info.Type.ToString()}");

                Console.WriteLine($"Devices: {info.MaxDevices.ToString()}");

            } else {

                switch (info.Usability) {

                    case LicenseUsability.NoConfiguredLicense:

                        Console.WriteLine("There's no configured license!");

                    case LicenseUsability.NotFound:

                        Console.WriteLine("The configured license is not valid " +

                            "because it doesn't exist in the database!");

                    case LicenseUsability.Expired:

                        Console.WriteLine("The configured license has expired!");

                }

            }

        case "1":

            LicenseInfo info;

            

            while (true) {

                Console.Write("Enter activation key> ");

                string input = Console.ReadLine();

                

                try {

                    info = client.ActivateProduct(input);

                } catch (FormatException) {

                    Console.WriteLine("The key format is invalid!");

                    continue;

                } catch (Exception ex) {

                    Console.WriteLine("An error occurred when looking up in the database!");

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

                

                break;

            }

            

            switch (info.Usability) {

                case LicenseUsability.Usable:

                    Console.WriteLine("License successfully applied! Expires at " +

                        info.Expiration.ToLongDateString());

                case LicenseUsability.TooManyDevices:

                    Console.WriteLine("The license is used by as many devices as it can " +

                        "be used by, thus a new device cannot use it!");

                case LicenseUsability.NotFound:

                    Console.WriteLine("The license is not valid " +

                        "because it doesn't exist in the database!");

                case LicenseUsability.Expired:

                    Console.WriteLine("The license has expired!");

                }

            }

    }

    

    break;

}

Console.ReadLine();
