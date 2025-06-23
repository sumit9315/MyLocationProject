using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hestia.LocationsMDM.WebApi.Controllers;
using Hestia.LocationsMDM.WebApi.Models;
using System;
using Hestia.LocationsMDM.WebApi.Exceptions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Test
{
    [TestClass]
    public class CampusControllerTest : BaseTest<CampusController>
    {
        [TestMethod]
        public void TestGetCampus1()
        {
            var result = _target.GetCampusAsync("CA00000100").Result;
            AssertResult(result);
        }

        [TestMethod]
        public void TestGetCampus2()
        {
            var result = _target.GetCampusAsync("CA00000200").Result;
            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestGetCampusFailure()
        {
            await _target.GetCampusAsync("fake");
        }

        [TestMethod]
        public async Task TestUpdateCampus()
        {
            var result = await _target.UpdateCampusAsync("CA00000100", new CampusPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true,
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "503-589-7397"
                },
                TimeZoneIdentifier = "UTC"
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestUpdateCampusFailure()
        {
            var result = await _target.UpdateCampusAsync("CA00000100", new CampusPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestUpdateCampusFailure2()
        {
            await _target.UpdateCampusAsync("fake", new CampusPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true,
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "503-589-7397"
                },
                TimeZoneIdentifier = "UTC"
            });
        }

        [TestMethod]
        public async Task TestPatchCampus01()
        {
            var result = await _target.PatchCampusAsync("CA00000100", new CampusPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true
            });

            AssertResult(result);
        }

        [TestMethod]
        public async Task TestPatchCampus02()
        {
            var result = await _target.PatchCampusAsync("CA00000100", new CampusPatchModel
            {
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "503-589-7397"
                },
                TimeZoneIdentifier = "UTC"
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestPatchCampusFailure()
        {
            await _target.PatchCampusAsync("fake", new CampusPatchModel
            {
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "503-589-7397"
                },
                TimeZoneIdentifier = "UTC"
            });
        }

        [TestMethod]
        public async Task TestUpdateCampusEvents01()
        {
            var result = await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = false,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "New Year Event",
                        EventStart = new RecurringDate
                        {
                            Month = 11,
                            DayOfMonth = 31,
                            DayOfWeek = null,
                            OrdinalInMonth = null
                        },
                        EventEnd = new RecurringDate
                        {
                            Month = 0,
                            DayOfMonth = 1,
                            DayOfWeek = null,
                            OrdinalInMonth = null
                        },
                        StartTime = new TimeOfDay
                        {
                            Hour = 18,
                            Minute = 45
                        },
                        EndTime = new TimeOfDay
                        {
                            Hour = 23,
                            Minute = 30
                        },
                        Frequency = "Yearly",
                        EventTime = "All Day",
                        DaylightSavings = true
                    },
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            Month = 10,
                            DayOfMonth = null,
                            DayOfWeek = 5,
                            OrdinalInMonth = 4
                        },
                        EventEnd = new RecurringDate
                        {
                            Month = 11,
                            DayOfMonth = null,
                            DayOfWeek = 1,
                            OrdinalInMonth = 1
                        },
                        StartTime = new TimeOfDay
                        {
                            Hour = 10,
                            Minute = 0
                        },
                        EndTime = new TimeOfDay
                        {
                            Hour = 19,
                            Minute = 0
                        },
                        Frequency = "Yearly",
                        EventTime = "Few Days",
                        DaylightSavings = false
                    }
                }
            });

            AssertResult(result);
        }

        [TestMethod]
        public async Task TestUpdateCampusEvents02()
        {
            var result = await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            Month = 10,
                            DayOfMonth = null,
                            DayOfWeek = 5,
                            OrdinalInMonth = 4
                        },
                        EventEnd = new RecurringDate
                        {
                            Month = 11,
                            DayOfMonth = null,
                            DayOfWeek = 1,
                            OrdinalInMonth = 1
                        },
                        StartTime = new TimeOfDay
                        {
                            Hour = 10,
                            Minute = 0
                        },
                        EndTime = new TimeOfDay
                        {
                            Hour = 19,
                            Minute = 0
                        },
                        Frequency = "Yearly",
                        EventTime = "Few Days",
                        DaylightSavings = false
                    }
                }
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure01()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            Month = -1,
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure02()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            Month = 13,
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure03()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            DayOfWeek = -1
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure04()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            DayOfWeek = 7
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure05()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            OrdinalInMonth = -1
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure06()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        EventStart = new RecurringDate
                        {
                            OrdinalInMonth = 5
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure07()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        StartTime = new TimeOfDay
                        {
                            Hour = -1
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure08()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        StartTime = new TimeOfDay
                        {
                            Hour = 24
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure09()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        StartTime = new TimeOfDay
                        {
                            Minute = -1
                        }
                    }
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure10()
        {
            await _target.UpdateCampusEventsAsync("CA00000100", new EventsCreateModel
            {
                ApplyToChildren = true,
                Events = new List<EventModel>
                {
                    new EventModel
                    {
                        EventName = "Black Friday",
                        StartTime = new TimeOfDay
                        {
                            Minute = 60
                        }
                    }
                }
            });
        }

        [TestMethod]
        public async Task CreateCampusRole()
        {
            var result = await _target.CreateCampusRoleAsync("CA00000100", new CampusRoleCreateModel
            {
                RoleName = "TestRole01"
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task CreateCampusRoleFailure01()
        {
            var result = await _target.CreateCampusRoleAsync("CA00000100", new CampusRoleCreateModel
            {
                RoleName = ""
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(DataConflictException))]
        public async Task CreateCampusRoleFailure02()
        {
            await _target.CreateCampusRoleAsync("CA00000100", new CampusRoleCreateModel
            {
                RoleName = "TestRole01"
            });

            await _target.CreateCampusRoleAsync("CA00000100", new CampusRoleCreateModel
            {
                RoleName = "TestRole01"
            });
        }

        [TestMethod]
        public async Task GetCampusRoles()
        {
            var result = await _target.GetCampusRolesAsync("CA00000100");
            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task GetCampusRolesFailure()
        {
            var result = await _target.GetCampusRolesAsync("fake");
            AssertResult(result);
        }

        [TestMethod]
        public async Task GetCampusRegion()
        {
            var result = await _target.GetCampusRegionAsync("CA00000100", "RG0000006");
            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task GetCampusRegionFailure01()
        {
            await _target.GetCampusRegionAsync("fake", "RG0000006");
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task GetCampusRegionFailure02()
        {
            await _target.GetCampusRegionAsync("CA00000100", "fake");
        }

        [TestMethod]
        public async Task TestUpdateCampusRegion()
        {
            var result = await _target.UpdateCampusRegionAsync("CA00000100", "RG0000006", new RegionPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true,
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "989-581-4214",
                    "503-589-7397"
                },
                Longitude = "50.470121142978506",
                Latitude = "30.604399537440685"
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestUpdateCampusRegionFailure()
        {
            await _target.UpdateCampusRegionAsync("CA00000100", "RG0000006", new RegionPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestUpdateCampusRegionFailure2()
        {
            await _target.UpdateCampusRegionAsync("CA00000100", "fake", new RegionPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true,
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "989-581-4214",
                    "503-589-7397"
                },
                Longitude = "50.470121142978506",
                Latitude = "30.604399537440685"
            });
        }

        [TestMethod]
        public async Task TestPatchCampusRegion01()
        {
            var result = await _target.PatchCampusRegionAsync("CA00000100", "RG0000006", new RegionPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true
            });

            AssertResult(result);
        }

        [TestMethod]
        public async Task TestPatchCampusRegion02()
        {
            var result = await _target.PatchCampusRegionAsync("CA00000100", "RG0000006", new RegionPatchModel
            {
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "989-581-4214",
                    "503-589-7397"
                },
                Longitude = "50.470121142978506",
                Latitude = "30.604399537440685"
            });

            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestPatchCampusRegionFailure()
        {
            await _target.PatchCampusRegionAsync("fake", "RG0000006", new RegionPatchModel
            {
                OpenForBusinessFlag = true,
                OpenForDisclosureFlag = true
            });
        }

        [TestMethod]
        public async Task AssignCampusAssociates01()
        {
            await _target.AssignCampusAssociatesAsync("CA00000100", new AssignAssociatesModel
            {
                ApplyToChildren = false,
                ContactList = new List<string>
                {
                    "2007202",
                    "1026258"
                }
            });

            var campus = await _target.GetCampusAsync("CA00000100");
            AssertResult(campus);
        }

        [TestMethod]
        public async Task AssignCampusAssociates02()
        {
            await _target.AssignCampusAssociatesAsync("CA00000100", new AssignAssociatesModel
            {
                ApplyToChildren = true,
                ContactList = new List<string>
                {
                    "1012842"
                }
            });

            var campus = await _target.GetCampusAsync("CA00000100");
            AssertResult(campus);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AssignCampusAssociatesFailure()
        {
            await _target.AssignCampusAssociatesAsync("CA00000100", new AssignAssociatesModel
            {
                ApplyToChildren = true,
                ContactList = new List<string> { }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task AssignCampusAssociatesFailure02()
        {
            await _target.AssignCampusAssociatesAsync("fake", new AssignAssociatesModel
            {
                ApplyToChildren = true,
                ContactList = new List<string>
                {
                    "1026258"
                }
            });
        }

        [TestMethod]
        public async Task UnassignCampusAssociates01()
        {
            await _target.UnassignCampusAssociatesAsync("CA00000100", new AssignAssociatesModel
            {
                ApplyToChildren = false,
                ContactList = new List<string>
                {
                    "1026258"
                }
            });

            var campus = await _target.GetCampusAsync("CA00000100");
            AssertResult(campus);
        }

        [TestMethod]
        public async Task UnassignCampusAssociates02()
        {
            await _target.UnassignCampusAssociatesAsync("CA00000100", new AssignAssociatesModel
            {
                ApplyToChildren = true,
                ContactList = new List<string>
                {
                    "2007202",
                    "1026258"
                }
            });

            var campus = await _target.GetCampusAsync("CA00000100");
            AssertResult(campus);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UnassignCampusAssociatesFailure()
        {
            await _target.UnassignCampusAssociatesAsync("CA00000100", new AssignAssociatesModel
            {
                ApplyToChildren = true
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task UnassignCampusAssociatesFailure02()
        {
            await _target.UnassignCampusAssociatesAsync("fake", new AssignAssociatesModel
            {
                ApplyToChildren = true,
                ContactList = new List<string>
                {
                    "1026258"
                }
            });
        }
    }
}
