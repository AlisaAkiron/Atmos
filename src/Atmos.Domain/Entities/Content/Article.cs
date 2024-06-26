﻿using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

[Table("article")]
public record Article : ContentBase
{
    [Column("is_draft")]
    public bool IsDraft { get; set; }

    [Column("classification")]
    public string Classification { get; set; } = string.Empty;
}
