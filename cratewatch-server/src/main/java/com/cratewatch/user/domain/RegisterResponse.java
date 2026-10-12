package com.cratewatch.user.domain;

import io.micronaut.serde.annotation.Serdeable;

import java.util.UUID;

@Serdeable
public record RegisterResponse(UUID id, String username) {
}
