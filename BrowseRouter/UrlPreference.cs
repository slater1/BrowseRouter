﻿namespace BrowseRouter
{
  public class UrlPreference
  {
    public required string UrlPattern { get; set; }
    public required Browser Browser { get; set; }

    public override string ToString() => $"\"{UrlPattern}\" => {Browser}";
  }
}
