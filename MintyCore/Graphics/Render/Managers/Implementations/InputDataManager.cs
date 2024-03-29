﻿using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IInputDataManager>(SingletonContextFlags.NoHeadless)]
internal class InputDataManager : IInputDataManager
{
    private readonly Dictionary<Identification, DictionaryInputData> _indexedInputData = new();
    private readonly Dictionary<Identification, SingletonInputData> _singletonInputData = new();


    public void RegisterKeyIndexedInputDataType(Identification id, DictionaryInputDataRegistryWrapper wrapper)
    {
        _indexedInputData.Add(id, wrapper.NewDictionaryInputData());
    }

    public IEnumerable<Identification> GetRegisteredInputDataIds()
    {
        return _indexedInputData.Keys.Concat(_singletonInputData.Keys);
    }

    public IEnumerable<Identification> GetUpdatedInputDataIds(bool reset)
    {
        List<Identification> result = new();

        foreach (var (id, data) in _indexedInputData)
        {
            if (data.WasModified)
                result.Add(id);

            if (reset)
                data.ResetModified();
        }

        foreach (var (id, data) in _singletonInputData)
        {
            if (data.WasModified)
                result.Add(id);

            if (reset)
                data.ResetModified();
        }

        return result;
    }

    public void UnRegisterInputDataType(Identification objectId)
    {
        _indexedInputData.Remove(objectId);
        _singletonInputData.Remove(objectId);
    }

    public void Clear()
    {
        _indexedInputData.Clear();
        _singletonInputData.Clear();
    }

    public void SetKeyIndexedInputData<TKey, TData>(Identification id, TKey key, TData data)
        where TKey : notnull
    {
        if (!_indexedInputData.TryGetValue(id, out var obj))
            throw new MintyCoreException($"No dictionary object found for {id}");

        if (obj is not DictionaryInputDataSet<TKey, TData> dic)
            throw new MintyCoreException(
                $"Type mismatch for {id}. Expected <{obj.KeyType.FullName}, {obj.DataType.FullName}> but got <{typeof(TKey).FullName}, {typeof(TData).FullName}>");

        dic.SetData(key, data);
    }
    
    public void RemoveKeyIndexedInputData<TKey>(Identification id, TKey key) where TKey : notnull
    {
        if (!_indexedInputData.TryGetValue(id, out var obj))
            throw new MintyCoreException($"No dictionary object found for {id}");

        if (obj is not DictionaryInputDataRemove<TKey> dic)
            throw new MintyCoreException(
                $"Type mismatch for {id}. Expected <{obj.KeyType.FullName}, {obj.DataType.FullName}> but got <{typeof(TKey).FullName}, object>");

        dic.RemoveData(key);
    }

    public SingletonInputData<TData> GetSingletonInputData<TData>(Identification inputDataId) where TData : notnull
    {
        if (!_singletonInputData.TryGetValue(inputDataId, out var inputData))
            throw new MintyCoreException($"Singleton Input Type for {inputDataId} is not registered");

        if (inputData is not SingletonInputData<TData> concreteInputData)
            throw new MintyCoreException(
                $"Wrong type for {inputDataId}, expected {inputData.DataType.FullName} but got {typeof(TData).FullName}");

        return concreteInputData;
    }

    public DictionaryInputData<TKey, TData> GetDictionaryInputData<TKey, TData>(Identification inputDataId)
        where TKey : notnull
    {
        if (!_indexedInputData.TryGetValue(inputDataId, out var inputData))
            throw new MintyCoreException($"Dictionary Input Type for {inputDataId} is not registered");

        if (inputData is not DictionaryInputData<TKey, TData> concreteInputData)
            throw new MintyCoreException(
                $"Wrong type for {inputDataId}, expected <{inputData.KeyType.FullName}, {inputData.DataType.FullName}> but got <{typeof(TKey).FullName}, {typeof(TData).FullName}>");

        return concreteInputData;
    }

    public void RegisterSingletonInputDataType(Identification id, SingletonInputDataRegistryWrapper wrapper)
    {
        _singletonInputData.Add(id, wrapper.NewSingletonInputData());
    }

    public void SetSingletonInputData<TDataType>(Identification id, TDataType data) where TDataType : notnull
    {
        if (!_singletonInputData.TryGetValue(id, out var inputData))
            throw new MintyCoreException($"Singleton Input Type for {id} is not registered");

        if (inputData is not SingletonInputData<TDataType> concreteInputData)
            throw new MintyCoreException(
                $"Wrong type for {id}, expected {inputData.DataType.FullName} but got {typeof(TDataType).FullName}");

        concreteInputData.SetData(data);
    }
}