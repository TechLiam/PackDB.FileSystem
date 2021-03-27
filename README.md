# PackDB File System [![Built and Tested](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/BuildAndTestAction.yml/badge.svg)](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/BuildAndTestAction.yml) [![CodeQL](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/TechLiam/PackDB.FileSystem/actions/workflows/codeql-analysis.yml)

PackDB File System is an extention on [PackDB Core](https://github.com/TechLiam/PackDB.Core) and added in the an implmentation that allows for data to be stored in a file system.

This project allows for data to be stored and optionally allows for data to be audited when created, update, deleted and restored. As well as allowing for indexing of the data for faster look ups of data in large data sets.

The data is stored with the file extention of ```.data``` and a file name of the id of the data.
The audit data is stored with the file extention of ```.audit``` and a file name of the id of the data matching the data file name.
The indexs are stored with the file extention of ```.index``` and a file name of the property name of the property used to index the data.

For each type of data a folder is created in a top level folder so an example of a database that stores school data would look like this:
- Data
  - Student
    - 1.data
    - 1.audit
    - 2.data
    - 2.audit
    - 3.data
    - 3.audit
    - Name.index
    - Age.index
  - Teachers
    - 1.data
    - 1.audit
    - 2.data
    - 2.audit
  - Classes
    - 1.data
    - 2.data
  - StudentToClasses
    - 1.data
    - 2.data

In the above example you can see how data is stored the top level folder can be configured.

# Get started
To get started is simple install the NuGet package and then create a data manager using the following:
``` csharp
DataManagerFactory.CreateFileSystemDataManager()
```
This default data manager is all setup and should work out of the box.

# What do my data models need
When creating data models such as Studen from the example above you need to inherit from the 
``` csharp 
DataEntity 
```
class this will give your data an Id property is a key of 1

## What is a key
A key is the way that MessagePack identified what property to put data back into when loading it back from the disk each property to be stored needs a key.
To add a key to a property you can simple add an attriute of
``` csharp
[Key(#)]
```
As you need to inherit from DataEntity you will need to start your keys at 2
