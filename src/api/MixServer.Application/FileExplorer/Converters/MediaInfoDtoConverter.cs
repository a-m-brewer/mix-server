﻿using System.Text;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Converters;

public interface IMediaInfoDtoConverter :
    IConverter<MediaInfo, MediaInfoDto>;

public class MediaInfoDtoConverter(INodePathDtoConverter nodePathDtoConverter) : IMediaInfoDtoConverter
{
    public MediaInfoDto Convert(MediaInfo value)
    {
        return new MediaInfoDto
        {
            Bitrate = value.Bitrate,
            Duration = FormatTimespan(value.Duration),
            NodePath = nodePathDtoConverter.ConvertToResponse(value.Path),
            Tracklist = value.Tracklist
        };
    }
    
    private static string FormatTimespan(TimeSpan duration)
    {
        var sb = new StringBuilder();

        var previousAdded = false;

        if (duration.Days > 0)
        {
            sb.Append($"{duration.Days:D1}.");
            previousAdded = true;
        }

        if (duration.Hours > 0 || previousAdded)
        {
            sb.Append($"{duration.Hours:D2}:");
            previousAdded = true;
        }

        if (duration.Minutes > 0 || previousAdded)
        {
            sb.Append($"{duration.Minutes:D2}:");
        }

        sb.Append($"{duration.Seconds:D2}");

        return sb.ToString();
    }
}