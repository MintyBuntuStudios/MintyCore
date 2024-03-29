﻿using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

/// <summary>
/// Interface for managing render input data
/// </summary>
[PublicAPI]
public interface IInputDataManager
{
    /// <summary>
    /// Set the current data for the given id
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="data"> The data to set </param>
    /// <typeparam name="TData"> The type of the data </typeparam>
    public void SetSingletonInputData<TData>(Identification id, TData data) where TData : notnull;

    /// <summary>
    ///  Set the current data for the given key for the given id
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="key"> The key of the data to set </param>
    /// <param name="data"> The data to set </param>
    /// <typeparam name="TKey"> The type of the key </typeparam>
    /// <typeparam name="TData"> The type of the data </typeparam>
    public void SetKeyIndexedInputData<TKey, TData>(Identification id, TKey key, TData data)
        where TKey : notnull;

    /// <summary>
    /// Removes the data associated with the given key for the given id.
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="key"> The key of the data to remove </param>
    /// <typeparam name="TKey"> The type of the key </typeparam>
    public void RemoveKeyIndexedInputData<TKey>(Identification id, TKey key) where TKey : notnull;

    /// <summary>
    ///  Get the current data for the given id
    /// </summary>
    /// <param name="inputDataId"> The id of the data </param>
    /// <typeparam name="TData"> The type of the data </typeparam>
    /// <returns> The current data </returns>
    public SingletonInputData<TData> GetSingletonInputData<TData>(Identification inputDataId)
        where TData : notnull;
    
    /// <summary>
    ///  Get the current data for the given key for the given id
    /// </summary>
    /// <param name="inputDataId"> The id of the data </param>
    /// <typeparam name="TKey"> The type of the key </typeparam>
    /// <typeparam name="TData"> The type of the data </typeparam>
    /// <returns> The current data </returns>
    public DictionaryInputData<TKey, TData> GetDictionaryInputData<TKey, TData>(Identification inputDataId)
        where TKey : notnull;

    /// <summary>
    /// Register a singleton input data type
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="wrapper"> The wrapper for the data </param>
    /// <remarks> Not intended to be called by user code </remarks>
    void RegisterSingletonInputDataType(Identification id, SingletonInputDataRegistryWrapper wrapper);
    
    /// <summary>
    ///  Register a key indexed input data type
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="wrapper"> The wrapper for the data </param>
    /// <remarks> Not intended to be called by user code </remarks>
    void RegisterKeyIndexedInputDataType(Identification id, DictionaryInputDataRegistryWrapper wrapper);
    
    /// <summary>
    ///  Get the ids of all registered input data
    /// </summary>
    /// <returns></returns>
    IEnumerable<Identification> GetRegisteredInputDataIds();
    
    /// <summary>
    ///  Get the ids of all input data that have been updated
    /// </summary>
    /// <param name="reset"> Whether to reset the updated status of the data </param>
    /// <returns> The ids of the updated data </returns>
    IEnumerable<Identification> GetUpdatedInputDataIds(bool reset);
    
    /// <summary>
    ///  Unregister a singleton input data type
    /// </summary>
    void UnRegisterInputDataType(Identification objectId);
    
    /// <summary>
    /// Clear all internal data
    /// </summary>
    void Clear();
}