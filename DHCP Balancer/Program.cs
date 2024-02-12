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
        public string ScopeId { get; set; }
        public string ClientId { get; set; } 
        public string IpAddress { get; set; } 
        public string Gateway { get; set; }
    }

    static void Main()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var scopes = config.GetSection("DhcpSettings:Scopes").Get<List<DhcpScope>>();
        var reservations = config.GetSection("DhcpSettings:Reservations").Get<List<DhcpReservation>>();

        Console.WriteLine("Select the operation you wish to perform:");
        Console.WriteLine("1. Update the gateway for each scope based on appsettings.json");
        Console.WriteLine("2. Modify the gateways for reservations individually");
        Console.Write("Enter your choice (1 or 2): ");

        var option = Console.ReadLine();

        using (PowerShell psInstance = PowerShell.Create())
        {
            psInstance.AddScript("Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -Force");
            psInstance.AddScript("Import-Module DhcpServer");

            switch (option)
            {
                case "1":
                    UpdateGatewaysForScopes(psInstance, scopes);
                    break;
                case "2":
                    UpdateReservationsGateways(psInstance, reservations);
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }

            ExecutePowerShellCommands(psInstance);
        }

        PromptToExit();
    }

    static void UpdateGatewaysForScopes(PowerShell ps, List<DhcpScope> scopes)
    {
        foreach (var scope in scopes)
        {
            ps.AddScript($"Set-DhcpServerv4OptionValue -ScopeId {scope.Id} -OptionId 3 -Value {scope.Gateway}");
        }
    }

    static void UpdateReservationsGateways(PowerShell ps, List<DhcpReservation> reservations)
    {
        foreach (var reservation in reservations)
        {
            string cmdSetGatewayOption = $"Set-DhcpServerv4OptionValue -IPAddress '{reservation.IpAddress}' -OptionId 3 -Value '{reservation.Gateway}'";
            ps.AddScript(cmdSetGatewayOption);
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
