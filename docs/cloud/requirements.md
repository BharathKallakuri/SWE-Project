# Software Requirements Specification (Draft)

Specifications for the cloud module.

# Features (based on priority)

We are going to `Azure SDK`.

1. `Persistence over Cloud`
- We may need to `authenticate the local application` to be able to use with cloud.
- We could add some `application secrets` in `Azure Key Vault` [Not final]
- Logging for Azure (in the SDK) 
- `Key - Value store` (client requests storing a value and we return a key which the client has to keep track of) (key can be UUID/ULID).

2. `Networking over Cloud`
- We will provide a class that is inherited from the `Networking` class that redirects the requests through the cloud instead of the server.
- If the local network fails, we will use `SignalR` to be able to send data through cloud.

We instead provided a subset of the Networking class with following main limitations: 
- No authentication on the server, 
- Injecting the current username into the sender id is not available (due to lack of time)
- Everyone inside and outside the app can broadcast (access control is not there)
- We could have moved the shared thigns between cloud.dll and cloud.networking.server.dll in one region.
- We have hardcoded the urls (a better way is not known).
 
3. `Crash Handling (with Insights)`
- Whenever it crashes, we will use our `persisted logs, stack (?) and some other details` and use `insights` api key to get the `reason for the crash` by prompting the respective LLM.
- (Optional) We will try creating a `self correcting build pipeline` (using insights).