package com.cratewatch.theaudiodb.domain;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import io.micronaut.serde.annotation.Serdeable;

import java.util.List;

@Serdeable
@JsonIgnoreProperties(ignoreUnknown = true)
public record TheAudioDbLookupResponse(List<TheAudioDbArtist> artists) {
}
