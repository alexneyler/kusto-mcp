# Kusto MCP Configuration

This project is designed to interact with Kusto databases and provides tools for querying and managing resources. Below are the steps to configure the project using the `settings.yaml` file.

## Configuration File: `settings.yaml`

The MCP server takes in a `--settings` argument that should point to a yaml file configured as follows:

### Model Configuration

```yaml
model:
  endpoint: <Azure OpenAI Endpoint>
  deployment: <Deployment Name>
```
- **endpoint**: The Azure OpenAI endpoint URL.
- **deployment**: The name of the deployment to use.

### Kusto Configuration

```yaml
kusto:
  - name: <Table Name>
    category: <Category>
    database: <Database Name>
    table: <Table Name>
    endpoint: <Kusto Endpoint>
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
- **prompts**: A list of prompts used for querying the Kusto database.
  - **type**: The type of the prompt (e.g., `system`, `user`, `assistant`).
  - **content**: The content of the prompt, which can include KQL queries and explanations.

### Example Configuration

Below is an example configuration for the `settings.yaml` file:

```yaml
model:
  endpoint: <endpoint-to-azure-openai-resource>
  deployment: <deployment-name>

kusto:
  - name: mytable
    category: mycategory
    database: mydatabase
    table: table
    endpoint: https://table.kusto.windows.net
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

## Notes

- Ensure that the `endpoint` URLs are correct and accessible. Authentication is done via the currently logged in user in Windows. You may need to run `az login` or ensure that you are logged into to VSCode/VS to be able to authenticate
- Update the `prompts` section with relevant queries and explanations as needed. The more descriptive the better.
- Use the example configuration as a template to set up your own `settings.yaml` file.
