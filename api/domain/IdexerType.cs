using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.domain;

/// <summary>
/// Tipo de indexador
/// </summary>
public enum IdexerType
{
    /// <summary>
    /// CDI
    /// </summary>
    Cdi = 0,


    /// <summary>
    /// IPCA + x%
    /// </summary>
    IpcaMais = 1,


    /// <summary>
    /// Percentual ao ano
    /// </summary>
    PercentYear = 2
}
