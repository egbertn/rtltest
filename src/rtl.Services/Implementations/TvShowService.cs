﻿using demo.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using TvMazeApi;
using TvMazeScraper.Entities;
using TvMazeScraper.Models;
using System.Diagnostics;

namespace demo.Services.Implementations
{
    public class TvShowService : ITvShowService
    {
        private readonly TvMazeClient _tvMazeClient;
        // other option was to use InMemorySql but this is much more easy
        public TvShowService(TvMazeClient tvMazeClient)
        {
            _tvMazeClient = tvMazeClient;
        }
        private int CacheKey(int pageNumber, int pageSize)
        {
            return  ((long)pageNumber | (long)pageSize << 32).GetHashCode();
        }
        public async Task<TvsShowsResponse> GetShowsWithActorAsync(int pageNumber, int pageSize = 50)
        {
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Must be greater or equal to 0");
            }
            if (pageSize > 250)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Must not exceed 250");
            }
            if (pageSize % 50 != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "must be dividable by 50");
            }


            var virtualPageSize = (pageNumber * pageSize) / 250D;
            var newTvShows = await _tvMazeClient.GetShowsAsync((int)Math.Floor(virtualPageSize));


            var virtualSkip = ((pageNumber) * pageSize) % 250;
            var response = new TvsShowsResponse(newTvShows.Skip(virtualSkip)
                // could be done using AutoMapper  for the sake of '3 hours' I skip this :)
                .Select(s => new TvShow()
                {
                    TvShowId = s.Id,
                    Name = s.Name
                }
                ).Take(pageSize));
            foreach (var tvShow in response.Shows.AsParallel())
            {
                Trace.TraceInformation($"Page: {pageNumber} show:{tvShow.Id} get cast");
                var cast = await _tvMazeClient.GetCastAsync(tvShow.Id);

                tvShow.Cast = cast.Select(s => new Actor()
                {
                    Name = s.Person.Name,
                    Id = s.Person.Id,
                    BirthDate = s.Person.Birthday

                }).Distinct().OrderByDescending(d => d.BirthDate).ToArray();
            }

            return response;
        }
    }
}