﻿namespace MixServer.Domain.Interfaces;

public interface IConverter {}

public interface IConverter<in TIn, out TOut> : IConverter
{
    TOut Convert(TIn value);
}

public interface IConverter<in TIn1, in TIn2, out TOut> : IConverter
{
    TOut Convert(TIn1 value, TIn2 value2);
}
