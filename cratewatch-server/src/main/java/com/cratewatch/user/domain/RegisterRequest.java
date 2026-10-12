package com.cratewatch.user.domain;

import io.micronaut.serde.annotation.Serdeable;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

@Serdeable
public record RegisterRequest(
    @NotBlank String username,
    @NotBlank @Size(min = 8) String password
) {
}
