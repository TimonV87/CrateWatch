package com.cratewatch.theaudiodb.domain;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import io.micronaut.serde.annotation.Serdeable;

@Serdeable
@JsonIgnoreProperties(ignoreUnknown = true)
public record TheAudioDbArtist(String strArtistThumb) {
}
