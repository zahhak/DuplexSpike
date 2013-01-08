Our current abstraction over SignalR provides execution of backend services from clients through common interfaces.

Every long running method in our abstraction returns an IOperationSession. Currently the server side handles session creating the following way - if the resolved ConnectionContext doesn't contain a session for the client's connectionId it creates one.

Operation completion is indicated by sending a json containing property OperationId. There are three types of operation completion jsons that can be currently sent across the wire:

    json indicating a successful Session creation: {"OperationId":"199c5b31-4463-4299-8e19-d1dbf95e50f8"}
    json indicating a sucessful operation: { "OperationId":"4df5d7d4-feb2-4549-a07a-6506128af388", "Result": SERIALIZED JSON RESULT }
    json indicating an error ocurred when executing the service with the given parameters { "OperationId":"4df5d7d4-feb2-4549-a07a-6506128af388", "Error": Error }

When calling methods from both ways(the server and the client) the following json is sent: (example) { "OperationId": "54d20b33-a742-4316-9b85-187e6764efab", "MethodName": "Build", "Parameters": { "buildData": { "MyProperty": null } } }
