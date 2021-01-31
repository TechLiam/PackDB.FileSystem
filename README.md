# PackDB
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

## DataManager / IDataManager
The DataManager is the entry point into PackDB and from a usage point of view the only thing you will ever need to work with.

The IDataManager interface allows you to call the following methods:
- Read
- Write
- Delete
- Restore

### Read
This method is intended to retreave and return the data.
There are three ways to read data from PackDB:
1. Read by Id
2. Read a collection of results by Id
3. Read a collection of results by Index

#### Read by Id

``` csharp
TDataType Read<TDataType>(int id) where TDataType : DataEntity;
```
This method allow a generic type of data to be retreaved as long as it inherits from DataEntity.
Read should return NULL if the data doesn't exist and the fully Deserialized data if it does.
