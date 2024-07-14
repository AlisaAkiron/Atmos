using Atmos.Domain.Entities.Content;
using Atmos.Services.Api.Common.Dto;
using Riok.Mapperly.Abstractions;

namespace Atmos.Services.Api.Common.Mapper;

[Mapper]
public static partial class ContentMapper
{
    public static partial ArticleDto MapArticleDto(this Article article);

    public static partial ArticleMetadataDto MapArticleMetadataDto(this Article article);
}
