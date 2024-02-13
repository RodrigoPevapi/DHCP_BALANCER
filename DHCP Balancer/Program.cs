using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using Microsoft.Extensions.Configuration;

class Program
{
    class DhcpScope
    {
        public string Id { get; set; }
        public string Gateway { get; set; }
    }

    class DhcpReservation
    {
        public string IpAddress { get; set; } 
        public string Gateway { get; set; }
    }

    static void Main()
    {
        Console.WriteLine("Select the operation you wish to perform:");
        Console.WriteLine("1. Update the gateway for scopes and reservations based on appsettings_scopes1.json");
        Console.WriteLine("2. Update the gateway for scopes and reservations based on appsettings_scopes2.json");
        Console.WriteLine("3. Modify the gateways for reservations individually based on appsettings_reservations.json");
        Console.Write("Enter your choice (1, 2, or 3): ");

        var option = Console.ReadLine();

        string configFile = "";
        switch (option)
        {
            case "1":
                configFile = "appsettings_scopes1.json";
                break;
            case "2":
                configFile = "appsettings_scopes2.json";
                break;
            case "3":
                configFile = "appsettings_reservations.json";
                break;
            default:
                Console.WriteLine("Invalid option.");
                PromptToExit();
                return;
        }

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFile, optional: true, reloadOnChange: true)
            .Build();

        var scopes = config.GetSection("DhcpSettings:Scopes").Get<List<DhcpScope>>();
        var reservations = config.GetSection("DhcpSettings:Reservations").Get<List<DhcpReservation>>();

        using (PowerShell psInstance = PowerShell.Create())
        {
            psInstance.AddScript("Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -Force");
            psInstance.AddScript("Import-Module DhcpServer");

            if (scopes != null && scopes.Count > 0)
            {
                UpdateGatewaysForScopes(psInstance, scopes);
            }

            if (reservations != null && reservations.Count > 0)
            {
                UpdateReservationsGateways(psInstance, reservations);
            }

            ExecutePowerShellCommands(psInstance);
        }

        PromptToExit();
    }

    static void UpdateGatewaysForScopes(PowerShell ps, List<DhcpScope> scopes)
    {
        if (scopes == null || scopes.Count == 0)
        {
            Console.WriteLine("No scopes to update.");
            return;
        }

        foreach (var scope in scopes)
        {
            if (!string.IsNullOrWhiteSpace(scope.Id) && !string.IsNullOrWhiteSpace(scope.Gateway))
            {
                ps.AddScript($"Set-DhcpServerv4OptionValue -ScopeId {scope.Id} -OptionId 3 -Value {scope.Gateway}");
                Console.WriteLine($"Updating scope '{scope.Id}' with new gateway '{scope.Gateway}'.");
            }
            else
            {
                Console.WriteLine($"Scope ID or Gateway is not set for one of the scopes. Skipping...");
            }
        }
    }

    static void UpdateReservationsGateways(PowerShell ps, List<DhcpReservation> reservations)
    {
        if (reservations == null || reservations.Count == 0)
        {
            Console.WriteLine("No reservations to update.");
            return;
        }

        foreach (var reservation in reservations)
        {
            if (!string.IsNullOrWhiteSpace(reservation.IpAddress) && !string.IsNullOrWhiteSpace(reservation.Gateway))
            {
                string cmdSetGatewayOption = $"Set-DhcpServerv4OptionValue -IPAddress '{reservation.IpAddress}' -OptionId 3 -Value '{reservation.Gateway}'";
                ps.AddScript(cmdSetGatewayOption);
                Console.WriteLine($"Updating reservation for IP '{reservation.IpAddress}' with new gateway '{reservation.Gateway}'.");
            }
            else
            {
                Console.WriteLine($"IP Address or Gateway is not set for one of the reservations. Skipping...");
            }
        }
    }

    static void ExecutePowerShellCommands(PowerShell ps)
    {
        var results = ps.Invoke();

        if (ps.HadErrors)
        {
            Console.WriteLine("Error while executing commands.");
            foreach (var error in ps.Streams.Error)
            {
                Console.WriteLine(error.Exception.Message);
            }
        }
        else
        {
            Console.WriteLine("Commands executed successfully.");
        }
    }

    static void PromptToExit()
    {
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}
