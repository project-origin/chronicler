using System;
using AutoFixture.Kernel;

namespace ProjectOrigin.Chronicler.Test.FixtureCustomizations;

public class MicrosecondDateTimeOffsetGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        // If the request is for a DateTimeOffset, return a DateTimeOffset with the ticks rounded to the nearest 10 microseconds.
        // This is because the PostgreSQL timestamp type only has microsecond precision.
        if (request is SeededRequest sr && sr.Request is Type t && t == typeof(DateTimeOffset))
        {
            var dateTimeOffset = (DateTimeOffset)context.Resolve(typeof(DateTimeOffset));
            return new DateTimeOffset(dateTimeOffset.Ticks / 10 * 10, dateTimeOffset.Offset);
        }

        return new NoSpecimen();
    }
}
