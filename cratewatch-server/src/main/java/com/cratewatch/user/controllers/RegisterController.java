package com.cratewatch.user.controllers;

import com.cratewatch.user.domain.User;
import com.cratewatch.user.domain.RegisterRequest;
import com.cratewatch.user.domain.RegisterResponse;
import com.cratewatch.user.repositories.UserRepository;
import io.micronaut.http.HttpResponse;
import io.micronaut.http.HttpStatus;
import io.micronaut.http.annotation.Body;
import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Post;
import io.micronaut.security.annotation.Secured;
import io.micronaut.security.rules.SecurityRule;
import jakarta.validation.Valid;
import lombok.AllArgsConstructor;
import org.mindrot.jbcrypt.BCrypt;

@AllArgsConstructor
@Secured(SecurityRule.IS_ANONYMOUS)
@Controller("/auth")
public class RegisterController {

    private final UserRepository userRepository;

    @Post("/register")
    public HttpResponse<RegisterResponse> register(@Body @Valid RegisterRequest request) {
        if (userRepository.existsByUsername(request.username())) {
            return HttpResponse.status(HttpStatus.CONFLICT);
        }
        String passwordHash = BCrypt.hashpw(request.password(), BCrypt.gensalt());
        User saved = userRepository.save(new User(request.username(), passwordHash));
        return HttpResponse.created(new RegisterResponse(saved.getId(), saved.getUsername()));
    }
}
