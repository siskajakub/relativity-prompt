# Relativity AI Prompt
Relativity mass event handled for AI document prompting.

# Install
## 1) Create Instance Settings
Create required Relativity Instance Settings entries:  
Name | Section | Value Type | Value (example) | Description
---- | ------- | ---------- | --------------- | -----------
DestinationField | Prompt | Text | Extracted Text Prompted | Document Field where to record the result of prompting.
LogField | Prompt | Text | Prompting Log | Document Field to store the prompting log.
Model | Prompt | Text | gpt-4 | Name of the model to use.
OpenAIKey | Prompt | Text | xxxxxxxxx | OpenAI API key.
OpenAIEndpoint | Prompt | Text | https://api.openai.com | OpenAI API endpoint.
Prompt | Prompt | Text | Please summarize following text: | Text of the prompt. Text of each document is appended after the prompt text.
PromptMaxSize | Prompt | Text | 10000 | Threshold for overall prompt size, longer text will be truncated. Number must be integer.
SourceField | Prompt | Text | Extracted Text | Document Field with the text to use for prompting.

## 2) Compile DLL
Download the source code and compile the code using Microsoft Visual Studio 2019.  
For more details on how to setup your development environemnt, please follow official [Relativity documentation](https://platform.relativity.com/10.3/index.htm#Relativity_Platform/Setting_up_your_development_environment.htm).  
You can also use precompiled DLL from the repository.

## 3) Upload DLL
Upload `RelativityPrompt.dll` to Relativity Resource Files.
You may need to install also additional libraries that are required. These libraries were required for Relativity Server 2022:
* Microsoft.Bcl.AsyncInterfaces.dll
* System.Buffers.dll
* System.Memory.dll
* System.Numerics.Vectors.dll
* System.Runtime.CompilerServices.Unsafe.dll
* System.Text.Encodings.Web.dll
* System.Text.Json.dll
* System.Threading.Tasks.Extensions.dll

## 4) Add to Workspace
For desired workspaces add mass event handler to Document Object:
* Browse to Document Object (Workspace->Workspace Admin->Object Type->Document)
* In Mass Operations section click New and add the handler:
  * Name: AI Prompt
  * Pop-up Directs To: Mass Operation Handler
  * Select Mass Operation Handler: RelativityPrompt.dll

# Log
Mass operation generates prompting log to fiels specified by the Relativity Instance Settings.  
Log entry is added after each prompting. There can be multiple log entries for one Document.  
Log entry has following fields:
* User email address
* Timestamp
* Model
* Character count of the prompt
* Character count of the result of the prompting

Prompting log can be viewed from the Relativity front-end via attached Relativity Script.

# Notes
Relativity AI Prompt mass operation was developed and tested in Relativity Server 2023.  
Relativity AI Prompt mass operation works correctly only with UTF-8 text.
