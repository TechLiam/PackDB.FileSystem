# PackDB [![Built and Tested](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/BuildAndTestAction.yml/badge.svg)](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/BuildAndTestAction.yml) [![CodeQL](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/codeql-analysis.yml)
PackDB was created to provide a .Net implementation of a data access layer that uses MessagePack as the data Serializer on the backend.

The current solution allows data to be stored using the following methods:
- OS File storage

The solution was built with extensibility in mind and should allow for other storage systems to be used.

## Why was this created?
PackDB was created as part of a separate project that aimed to use local disk storage as the primary data repository. Two of the projects' main goals were to store data with a minimal footprint and not be reliant on any other technologies.

## Who can use this?
Anyone can use PackDB if they wish it is published under the MIT licence and you use at your own risk.

## Can I add to the project?
Yes, please feel free to submit push requests to this repo or create stand-alone extension projects that can store data to other storage systems.

## Documentaton
We will be adding documentation on every aspect of the PackDB solution in the [Wiki](https://github.com/TechLiam/PackDB/wiki) section

## Help
If you need help using this solution please create an issue in the issue tab and ask your question we will be happy to help
