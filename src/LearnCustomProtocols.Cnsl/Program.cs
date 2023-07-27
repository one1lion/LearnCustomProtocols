using LearnCustomProtocols.Shared;

const string _protocolName = "myCustConsoleProt";

var custProtocol = new CustomProtocol(_protocolName);

do
{
    Console.Write(
@$"What do?
    r - Register the Custom Protocol {_protocolName}
    u - Unregister the Custom Protocol
    q or leave blank to quit

Make a selection by entering the corresponding character: "
    );
    var input = Console.In.ReadLine();
    switch (input?.Trim()?.ToLower())
    {
        case "r":
            try
            {
                custProtocol.RegisterUrlProtocol();
                Console.WriteLine($"Successfully registered the custom protocol.  Use `start {_protocolName}://` to try it out.  You can also type `{_protocolName}://` into your browser.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Registration failed.");
                Console.WriteLine();
                Console.WriteLine("Exception Info:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            break;
        case "u":
            try
            {
                custProtocol.UnregisterUrlProtocol();
                Console.WriteLine("Successfully unregistered the custom protocol.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(@$"Failed to unregister the custom protocol. Although...it's still possible that the protocol was successfully unregistered. There may be an issue somewhere else. To verify, try using the protocol. Windows should inform you that you do not have an application to handle the link.");
                Console.WriteLine();
                Console.WriteLine("Exception Info:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            break;
        case "q":
        case "":
        case null:
            return -1;
        default:
            Console.WriteLine("Invalid input.  Please try again.");
            break;
    }
    Console.WriteLine();
    Console.WriteLine();
} while (true);
