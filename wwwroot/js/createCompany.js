document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("createCompanyForm");

    form.addEventListener("submit", function (event) {

        var companyName = document.getElementById("companyName");
        


        var description = document.getElementById("description");

        var phone = document.getElementById("phone");

        var email = document.getElementById("email");

        var address = document.getElementById("address");

        var isValid = true;

        // Clear previous error messages
        document.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
        });

        // Validate company name
        if (companyName.value.trim() === "") {
            showError("Name", "Company name is required.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            companyName.style.borderColor = "red";
            isValid = false;
        }
        else if (companyName.value.trim().length < 3) {
            showError("Name", "Company name must be at least 3 characters.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            companyName.style.borderColor = "red";
            isValid = false;
        }

        else {

            document.getElementById("nameStart").textContent = "";
            companyName.style.borderColor = "";
        }

        // Validate description
        if (description.value.trim().length > 2000) {
            showError("Description", "Company description cannot be more than 2000 characters.");
            document.getElementById("descStart").textContent = "*";
            document.getElementById("descStart").style.color = "red";
            description.style.borderColor = "red";

            isValid = false;
        }
        else {
            document.getElementById("descStart").textContent = "";
            description.style.borderColor = "";

        }

        // Validate phone number
        var phonePattern = /^\+?[0-9\s\-()]{7,20}$/;

        if (phone.value.trim() === "") {
            showError("Phone", "Phone number is required.");
            document.getElementById("phoneStart").textContent = "*";
            document.getElementById("phoneStart").style.color = "red";
            phone.style.borderColor = "red";
            isValid = false;
        }
        else if (!phonePattern.test(phone.value.trim())) {
            showError("Phone", "Enter a valid phone number.");
            document.getElementById("phoneStart").textContent = "*";
            document.getElementById("phoneStart").style.color = "red";
            phone.style.borderColor = "red";
            isValid = false;
        }
        else {

            document.getElementById("phoneStart").textContent = "";
            phone.style.borderColor = "";
        }

        // Validate email
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (email.value.trim() === "") {
            showError("Email", "Email address is required.");
            document.getElementById("emailStart").textContent = "*";
            document.getElementById("emailStart").style.color = "red";
            email.style.borderColor = "red";
            isValid = false;
        }
        else if (!emailPattern.test(email.value.trim())) {
            showError("Email", "Enter a valid email address.");
            document.getElementById("emailStart").textContent = "*";
            document.getElementById("emailStart").style.color = "red";
            email.style.borderColor = "red";

            isValid = false;
        }
        else {

            document.getElementById("emailStart").textContent = "";
            email.style.borderColor = "";
        }

        // Validate address
        if (address.value.trim() === "") {
            showError("Address", "Company address is required.");
            document.getElementById("addressStart").textContent = "*";
            document.getElementById("addressStart").style.color = "red";
            address.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("addressStart").textContent = "*";
            address.style.borderColor = "";
        }

        // Prevent form submission if validation fails
        if (!isValid) {
            event.preventDefault();
        }
    });

    // Display error message below the field
    function showError(fieldName, message) {
        var errorSpan = document.querySelector('[data-valmsg-for="' + fieldName + '"]');

        if (errorSpan != null) {
            errorSpan.textContent = message;
            errorSpan.classList.remove("field-validation-valid");
            errorSpan.classList.add("field-validation-error");
        }
    }
});