package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;

/**
 * Single place for legacy wall-clock → OffsetDateTime conversion.
 * The declared source timezone is Europe/Berlin.
 */
public final class TimestampConverter {

    private static final ZoneId SOURCE_ZONE = ZoneId.of("Europe/Berlin");

    private TimestampConverter() {}

    /**
     * Converts a wall-clock LocalDateTime (assumed Europe/Berlin) to an OffsetDateTime
     * that correctly reflects DST.
     */
    public static OffsetDateTime toOffset(LocalDateTime wallClock) {
        if (wallClock == null) {
            return null;
        }
        return wallClock.atZone(SOURCE_ZONE).toOffsetDateTime();
    }

    /**
     * Parses an ISO-8601 date-time string (no zone) and converts it.
     */
    public static OffsetDateTime parse(String text) {
        if (text == null || text.isBlank()) {
            return null;
        }
        LocalDateTime ldt = LocalDateTime.parse(text, DateTimeFormatter.ISO_LOCAL_DATE_TIME);
        return toOffset(ldt);
    }
}
