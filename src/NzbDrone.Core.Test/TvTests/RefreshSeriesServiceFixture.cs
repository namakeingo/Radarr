﻿using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Commands;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.TvTests
{
    [TestFixture]
    public class RefreshSeriesServiceFixture : CoreTest<RefreshSeriesService>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            var season1 = Builder<Season>.CreateNew()
                                         .With(s => s.SeasonNumber = 1)
                                         .Build();

            _series = Builder<Series>.CreateNew()
                                     .With(s => s.Seasons = new List<Season>
                                                            {
                                                                season1
                                                            })
                                     .Build();

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(_series.Id))
                  .Returns(_series);

            
        }

        private void GivenNewSeriesInfo(Series series)
        {
            Mocker.GetMock<IProvideSeriesInfo>()
                  .Setup(s => s.GetSeriesInfo(It.IsAny<Int32>()))
                  .Returns(new Tuple<Series, List<Episode>>(series, new List<Episode>()));
        }

        [Test]
        public void should_monitor_new_seasons_automatically()
        {
            var series = _series.JsonClone();
            series.Seasons.Add(Builder<Season>.CreateNew()
                                         .With(s => s.SeasonNumber = 2)
                                         .Build());

            GivenNewSeriesInfo(series);

            Subject.Execute(new RefreshSeriesCommand(_series.Id));

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.UpdateSeries(It.Is<Series>(s => s.Seasons.Count == 2 && s.Seasons.Single(season => season.SeasonNumber == 2).Monitored == true)));
        }

        [Test]
        public void should_not_monitor_new_special_season_automatically()
        {
            var series = _series.JsonClone();
            series.Seasons.Add(Builder<Season>.CreateNew()
                                         .With(s => s.SeasonNumber = 0)
                                         .Build());

            GivenNewSeriesInfo(series);

            Subject.Execute(new RefreshSeriesCommand(_series.Id));

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.UpdateSeries(It.Is<Series>(s => s.Seasons.Count == 2 && s.Seasons.Single(season => season.SeasonNumber == 0).Monitored == false)));
        }
    }
}
