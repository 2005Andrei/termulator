namespace termulator.ViewModels;

public class Engine {
    private string image = "alpine:linux";

    public Engine() {
        // start engine
    }

    public string executeCommand(string Command) {
        // execute command
        return "something";
    }

    public void shutoff() {
        // close and delete docker container
    }
}
