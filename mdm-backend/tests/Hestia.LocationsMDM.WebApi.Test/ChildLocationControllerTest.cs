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
    public class ChildLocationControllerTest : BaseTest<ChildLocationController>
    {
        private const string DefaultLocationId = "CH123452";
        private const string FakeLocationId = "fake";

        [TestMethod]
        public async Task TestSearch1()
        {
            var result = await _target.SearchAsync();
            AssertResult(result);
        }

        [TestMethod]
        public async Task TestSearch2()
        {
            var result = await _target.SearchAsync(location: "Phoenix");
            AssertResult(result);
        }

        [TestMethod]
        public async Task TestSearch3()
        {
            var result = await _target.SearchAsync(location: "Phoenix", pageNum: 2, pageSize: 10);
            AssertResult(result);
        }

        [TestMethod]
        public async Task TestSearch4()
        {
            var result = await _target.SearchAsync(
                location: "Phoenix",
                address: "Buckeye",
                state: "AZ",
                zipCode: "85004",
                city: "Phoenix",
                lobCc: "TRAINEES",
                district: "Southwest",
                region: "",
                locationType: "");

            AssertResult(result);
        }

        [TestMethod]
        public async Task TestGet1()
        {
            var result = await _target.GetAsync(DefaultLocationId);
            AssertResult(result);
        }

        [TestMethod]
        public async Task TestGet2()
        {
            var result = await _target.GetAsync("CO00000100");
            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestGetFailure()
        {
            await _target.GetAsync(FakeLocationId);
        }

        [TestMethod]
        public async Task TestPatch01()
        {
            var result = await _target.PatchAsync(DefaultLocationId, new ChildLocationPatchModel
            {
                Address = CreateTestNodeAddress(1),
                AssociationName = "association Name 01",
                ProductOffering = new List<string>
                {
                    "PO 01",
                    "PO 02"
                },
                Longitude = "52.345121142978506",
                Latitude = "21.028899537440685",
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "503-589-7397"
                },
                RegionId = "RG0000004"
            });

            result.LastUpdatedOn = DateTime.MinValue;
            AssertResult(result);
        }

        [TestMethod]
        public async Task TestPatch02()
        {
            var result = await _target.PatchAsync(DefaultLocationId, new ChildLocationPatchModel
            {
                Address = CreateTestNodeAddress(2)
            });

            result.LastUpdatedOn = DateTime.MinValue;
            AssertResult(result);
        }

        [TestMethod]
        public async Task TestPatch03()
        {
            var result = await _target.PatchAsync(DefaultLocationId, new ChildLocationPatchModel
            {
                Longitude = "52.345121142978506",
                Latitude = "21.028899537440685",
                PhoneNumbers = new List<string>
                {
                    "313-599-4676",
                    "503-589-7397"
                }
            });

            result.LastUpdatedOn = DateTime.MinValue;
            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestPatchFailureArgNull()
        {
            await _target.PatchAsync(DefaultLocationId, null);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestPatchFailureNotFound()
        {
            await _target.PatchAsync(FakeLocationId, new ChildLocationPatchModel());
        }

        [TestMethod]
        public async Task TestDelete1()
        {
            var result = await _target.DeleteAsync(DefaultLocationId);
            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestDeleteShouldNotFind()
        {
            var result = await _target.DeleteAsync(DefaultLocationId);
            await _target.GetAsync(DefaultLocationId);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestDeleteFailureNotFound()
        {
            await _target.DeleteAsync(FakeLocationId);
        }

        [TestMethod]
        public async Task TestGetAssociates()
        {
            var result = await _target.GetAssociatesAsync(DefaultLocationId);
            AssertResult(result);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestGetAssociatesFailureNotFound()
        {
            await _target.DeleteAsync(FakeLocationId);
        }

        [TestMethod]
        public async Task TestAssignAssociates()
        {
            var model = new AssignAssociatesModel
            {
                ContactList = new List<string>
                {
                    "2007202",
                    "1026258"
                }
            };

            var apiResult = await _target.AssignAssociatesAsync(DefaultLocationId, model);
            var updatedAssociates = await _target.GetAssociatesAsync(DefaultLocationId);

            AssertResult(new
            {
                apiResult,
                updatedAssociates
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestAssignAssociatesFailureNotFound()
        {
            var model = new AssignAssociatesModel
            {
                ContactList = new List<string>
                {
                    "2007202"
                }
            };

            await _target.AssignAssociatesAsync(FakeLocationId, model);
        }

        [TestMethod]
        public async Task TestUnassignAssociates()
        {
            // arrange
            var model = new AssignAssociatesModel
            {
                ContactList = new List<string>
                {
                    "2007202",
                    "1026258"
                }
            };
            await _target.AssignAssociatesAsync(DefaultLocationId, model);

            // remove one item
            model.ContactList.Remove(model.ContactList[0]);

            var apiResult = await _target.UnassignAssociatesAsync(DefaultLocationId, model);
            var updatedAssociates = await _target.GetAssociatesAsync(DefaultLocationId);

            AssertResult(new
            {
                apiResult,
                updatedAssociates
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestUnassignAssociatesFailureNotFound()
        {
            var model = new AssignAssociatesModel
            {
                ContactList = new List<string>
                {
                    "2007202"
                }
            };

            await _target.UnassignAssociatesAsync(FakeLocationId, model);
        }

        [TestMethod]
        public async Task TestUpdateEvents01()
        {
            var model = new EventsCreateModel
            {
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
            };

            var apiResult = await _target.UpdateEventsAsync(DefaultLocationId, model);
            var updatedLocation = await _target.GetAsync(DefaultLocationId);
            updatedLocation.LastUpdatedOn = DateTime.MinValue;
            foreach (var item in updatedLocation.CalendarEvents)
            {
                item.EventId = Guid.Empty.ToString();
            }

            AssertResult(new
            {
                apiResult,
                updatedLocation
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task TestUpdateEventsFailureNotFound()
        {
            var model = new EventsCreateModel
            {
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
                    }
                }
            };

            await _target.UpdateEventsAsync(FakeLocationId, model);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestUpdateCampusEventsFailure01()
        {
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
            await _target.UpdateEventsAsync(DefaultLocationId, new EventsCreateModel
            {
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
        public async Task PatchBusinessInfo01()
        {
            var model = new BusinessInfoUpdateExtendedModel
            {
                ApplyToChildren = false,
                BusinessInfo = new BusinessInfoUpdateModel
                {
                    OpenForBusiness = true,
                    OpenForPurchase = false,
                    OpenForShipping = true,
                    OpenForReceiving = true,
                    OpenForDisclosure = false,
                    LastUpdatedTimestamp = DateTime.Today.AddDays(-1)
                }
            };

            var apiResult = await _target.PatchBusinessInfoAsync(DefaultLocationId, model);
            var updatedLocation = await _target.GetAsync(DefaultLocationId);
            updatedLocation.LastUpdatedOn = DateTime.MinValue;

            AssertResult(new
            {
                apiResult,
                updatedLocation
            });
        }

        [TestMethod]
        public async Task PatchBusinessInfo02()
        {
            var model = new BusinessInfoUpdateExtendedModel
            {
                ApplyToChildren = false,
                BusinessInfo = new BusinessInfoUpdateModel
                {
                    ProPickUp = true,
                    SelfCheckout = true,
                    StaffPropickup = false,
                    VisibleToWebsite = true,
                    LastUpdatedTimestamp = DateTime.Today.AddDays(-1)
                }
            };

            var apiResult = await _target.PatchBusinessInfoAsync(DefaultLocationId, model);
            var updatedLocation = await _target.GetAsync(DefaultLocationId);
            updatedLocation.LastUpdatedOn = DateTime.MinValue;

            AssertResult(new
            {
                apiResult,
                updatedLocation
            });
        }

        [TestMethod]
        public async Task PatchBusinessInfo03()
        {
            var model = new BusinessInfoUpdateExtendedModel
            {
                ApplyToChildren = true,
                BusinessInfo = new BusinessInfoUpdateModel
                {
                    OpenForBusiness = true,
                    OpenForPurchase = false,
                    OpenForShipping = true,
                    OpenForReceiving = false,
                    ProPickUp = true,
                    SelfCheckout = true,
                    StaffPropickup = false,
                    VisibleToWebsite = true,
                    OpenForDisclosure = false,
                    LastUpdatedTimestamp = DateTime.Today.AddDays(-1)
                }
            };

            var apiResult = await _target.PatchBusinessInfoAsync(DefaultLocationId, model);
            var updatedLocation = await _target.GetAsync(DefaultLocationId);
            updatedLocation.LastUpdatedOn = DateTime.MinValue;

            AssertResult(new
            {
                apiResult,
                updatedLocation
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task PatchBusinessInfoFailureNotFound()
        {
            var model = new BusinessInfoUpdateExtendedModel
            {
                BusinessInfo = new BusinessInfoUpdateModel
                {
                    LastUpdatedTimestamp = DateTime.Today.AddDays(-1)
                }
            };

            await _target.PatchBusinessInfoAsync(FakeLocationId, model);
        }

        [TestMethod]
        public async Task UpdateOperatingHours()
        {
            var model = new OperatingHoursUpdateModel
            {
                ApplyToChildren = false,
                OperatingHours = new List<OperatingHoursModel>
                {
                    CreateOperatingHoursModel(1),
                    CreateOperatingHoursModel(2),
                    CreateOperatingHoursModel(3),
                    CreateOperatingHoursModel(4),
                    CreateOperatingHoursModel(5)
                }
            };

            var apiResult = await _target.UpdateOperatingHoursAsync(DefaultLocationId, model);
            var updatedLocation = await _target.GetAsync(DefaultLocationId);
            updatedLocation.LastUpdatedOn = DateTime.MinValue;

            AssertResult(new
            {
                apiResult,
                updatedLocation
            });
        }

        [TestMethod]
        public async Task UpdateOperatingHoursApplyToChildren()
        {
            var model = new OperatingHoursUpdateModel
            {
                ApplyToChildren = true,
                OperatingHours = new List<OperatingHoursModel>
                {
                    CreateOperatingHoursModel(1),
                    CreateOperatingHoursModel(3),
                    CreateOperatingHoursModel(5),
                    CreateOperatingHoursModel(7)
                }
            };

            var apiResult = await _target.UpdateOperatingHoursAsync(DefaultLocationId, model);
            var updatedLocation = await _target.GetAsync(DefaultLocationId);
            updatedLocation.LastUpdatedOn = DateTime.MinValue;

            AssertResult(new
            {
                apiResult,
                updatedLocation
            });
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotFoundException))]
        public async Task UpdateOperatingHoursFailureNotFound()
        {
            var model = new OperatingHoursUpdateModel
            {
                OperatingHours = new List<OperatingHoursModel>
                {
                    CreateOperatingHoursModel(1)
                }
            };

            await _target.UpdateOperatingHoursAsync(FakeLocationId, model);
        }
    }
}
