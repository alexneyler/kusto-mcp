# Natural Language to Kusto Query MCP Server
A configurable MCP server that can convert natural language into kusto queries and execute the query against kusto database.

## Features
* Fully configurable: provide a `settings.yaml` file to give the kusto query generator the data it needs to generate queries against any database
* Generate: returns a kusto query based on the given natural language prompt
* Execute: generate and execute a query based on the given natural language prompt. Output responses as JSON or in a CSV file
* Multiple databases: configure multiple databases in `settings.yaml`

## Tools
* list-supported-tables
  * Lists the supported tables as provided in the `settings.yaml` file
* generate-kusto-query
  * Generates a kusto query against the provided table for the given prompt
  * Inputs:
    * `parameters`: an object containing the following parameters:
      * `table` (string): name of the table as configured in the `settings.yaml` file
      * `category` (string): category of the table as configured in the `settings.yaml` file
      * `prompt` (string): natural language prompt to execute against the provided database
* execute-kusto-query
*  Generates a kusto query against the provided table for the given prompt
  * Inputs:
    * `parameters`: an object containing the following parameters:
      * `table` (string): name of the table as configured in the `settings.yaml` file
      * `category` (string): category of the table as configured in the `settings.yaml` file
      * `prompt` (string): natural language prompt to execute against the provided database
      * `outputType` ('Json' or 'CSV'): output type for the response

## Configuration

This project is designed to interact with Kusto databases and provides tools for querying and managing resources. Below are the steps to configure the project using the `settings.yaml` file.

### Configuration File: `settings.yaml`

The MCP server takes in a `--settings` argument that should point to a yaml file configured as follows:

#### Environment variable references

You can configure references to environment variables in your settings.yaml file by using the `${{ENVIRONMENT_VARIABLE_NAME}}` syntax.

#### Model Configuration

```yaml
model:
  endpoint: <Azure OpenAI Endpoint>
  deployment: <Deployment Name>
  key: <Azure Open AI key>
```
- **endpoint**: The Azure OpenAI endpoint URL.
- **deployment**: The name of the deployment to use.
- **key** (optional): An optional key for the given Azure OpenAI endpoint. If a key is not provided, the tool will attempt to use the default azure credentials to execute on behalf of the user. This fallback currently only works if you run the server using `dotnet run` instead of `docker run`, and only inside of vs code.

#### Kusto Configuration

```yaml
kusto:
  - name: <Table Name>
    category: <Category>
    database: <Database Name>
    table: <Table Name>
    endpoint: <Kusto Endpoint>
    accessToken: <Access Token>
    prompts:
      - type: <Prompt Type>
        content: |
          <Prompt Content>
```
- **name**: The name of the Kusto table.
- **category**: The category of the Kusto table (e.g., `managedlabs`).
- **database**: The name of the Kusto database.
- **table**: The name of the table in the Kusto database.
- **endpoint**: The Kusto endpoint URL.
- **accessToken** (optional): An optional user access token that can be used to connect to the given kusto database. If an access token is not provided, the tool will attempt to use the default azure credentials to execute on behalf of the user. This fallback currently only works if you run the server using `dotnet run` instead of `docker run`, and only inside of vs code. To generate an access token for a kusto database, you can run the following:
  ```ps
  az login
  az account get-access-token --resource "your-kusto-database-url" --query "accessToken"
  ```
- **prompts**: A list of prompts used for querying the Kusto database.
  - **type**: The type of the prompt (e.g., `system`, `user`, `assistant`).
  - **content**: The content of the prompt, which can include KQL queries and explanations.

#### Example Configuration

Below is an example configuration for the `settings.yaml` file:

```yaml
model:
  endpoint: https://my-oai-resource.openai.azure.com
  deployment: my-deployment
  key: ${{ AZURE_OPENAI_KEY }}

kusto:
  - name: mytable
    category: mycategory
    database: mydatabase
    table: table
    endpoint: https://table.kusto.windows.net
    accessToken: ${{ MYTABLE_KUSTO_ACCESS_TOKEN }}
    prompts:
      - type: system
        content: |
          The table contains the following columns:
          * Id: id of the resource
          * Name: name of the resource
          * CreationTime: timestamp when the resource was created
          * LastModified: timestamp when the resource was last modified
          * Owner: owner of the resource
      - type: user
        content: When was the resource 'my resource' created in mycategory/mytable?
      - type: assistant
        content: |
          table
          | where name == 'my resource'
          | project CreationTime
      - type: user
        content: How many resources were created after April 1st, 2025?
      - type: assistant
        content: |
          table
          | where CreationTime > datetime(2025-04-01)
          | summarize count()
      - type: user
        content: Which owner owns the most resources in mycategory/mytable?
      - type: assistant
        content: |
          table
          | summarize Resources=count() by Owner
          | order by Resources desc
          | take 1
          | project Owner
```

#### Notes

- Ensure that the `endpoint` URLs are correct and accessible. Authentication is done via the currently logged in user in Windows. You may need to run `az login` or ensure that you are logged into to VSCode/VS to be able to authenticate
- Update the `prompts` section with relevant queries and explanations as needed. The more descriptive the better.
- Use the example configuration as a template to set up your own `settings.yaml` file.


### Usage with VS Code
Add this to your `.vscode/mcp.json`:
```jsonc
{
  "inputs": [
    {
        "type": "promptString",
        "id": "azure-open-ai-key",
        "description": "Enter your Azure OpenAI key",
        "password": true
    },
    {
        "type": "promptString",
        "id": "kusto-token",
        "description": "Enter your Kusto token",
        "password": true
    }
  ],
  "kusto": {
    "type": "stdio",
    "command": "docker",
    "args": [
        "run",
        "-i",
        "--rm",
        "-v",
        "/path/to/settings.yaml:/app/settings.yaml", // Enter path to settings.yaml file. Can use vscode variables
        // Include environment variables that are references in settings.yaml file
        "-e",
        "AZURE_OPENAI_KEY",
        "-e",
        "KUSTO_ACCESS_TOKEN",
        "alexeyler/kusto-mcp-server",
    ],
    "env": {
        "AZURE_OPENAI_KEY": "${input:azure-open-ai-key}",
        "KUSTO_ACCESS_TOKEN": "${input:kusto-token}",
    }
  }
}
```

## Build
From repo root:
`docker build ./src/Server/Dockerfile src`