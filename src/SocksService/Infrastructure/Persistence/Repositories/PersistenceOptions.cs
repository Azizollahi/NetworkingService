// Copyright By Hossein Azizollahi All Right Reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AG.RouterService.SocksService.Application.Abstractions.Authentication;
using AG.RouterService.SocksService.Domain;
using Microsoft.Extensions.Options;

namespace AG.RouterService.SocksService.Infrastructure.Persistence.Repositories;

public sealed class PersistenceOptions
{
	public string FilePath { get; set; } = "./configs/";
}
