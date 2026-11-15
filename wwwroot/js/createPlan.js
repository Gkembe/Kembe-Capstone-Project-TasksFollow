
document.addEventListener("DOMContentLoaded", function () {

    var form = document.querySelector("#createPlanForm, #editPlanForm");

    if (form == null) {
        return;
    }

    form.addEventListener("submit", function (event) {

        var planName = document.getElementById("planName");
        var description = document.getElementById("description");
        var monthlyPrice = document.getElementById("monthlyPrice");

        var isValid = true;

        // Clear previous error messages
        form.querySelectorAll(".field-validation-error, .field-validation-valid").forEach(function (span) {
            span.textContent = "";
            span.classList.remove("field-validation-error");
            span.classList.add("field-validation-valid");
        });

        // Validate plan name
        if (planName.value.trim() === "") {
            showError("Name", "Plan name is required.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            planName.style.borderColor = "red";
            isValid = false;
        }
        else if (planName.value.trim().length < 3) {
            showError("Name", "Plan name must be at least 3 characters.");
            document.getElementById("nameStart").textContent = "*";
            document.getElementById("nameStart").style.color = "red";
            planName.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("nameStart").textContent = "";
            planName.style.borderColor = "";
        }

        // Validate description
        if (description.value.trim() === "") {
            showError("Description", "Plan description is required.");
            document.getElementById("descStart").textContent = "*";
            document.getElementById("descStart").style.color = "red";
            description.style.borderColor = "red";
            isValid = false;
        }
        else if (description.value.trim().length > 2000) {
            showError("Description", "Plan description cannot be more than 2000 characters.");
            document.getElementById("descStart").textContent = "*";
            document.getElementById("descStart").style.color = "red";
            description.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("descStart").textContent = "";
            description.style.borderColor = "";
        }

        // Validate monthly price
        if (monthlyPrice.value.trim() === "") {
            showError("MonthlyPrice", "Monthly price is required.");
            document.getElementById("priceStart").textContent = "*";
            document.getElementById("priceStart").style.color = "red";
            monthlyPrice.style.borderColor = "red";
            isValid = false;
        }
        else if (!Number.isFinite(Number(monthlyPrice.value)) || Number(monthlyPrice.value) < 0.01 || Number(monthlyPrice.value) > 100000) {
            showError("MonthlyPrice", "Monthly price must be between 0.01 and 100000.");
            document.getElementById("priceStart").textContent = "*";
            document.getElementById("priceStart").style.color = "red";
            monthlyPrice.style.borderColor = "red";
            isValid = false;
        }
        else if (!/^\d+(\.\d{1,2})?$/.test(monthlyPrice.value)) {
            showError("MonthlyPrice", "Monthly price cannot have more than 2 decimal places.");
            document.getElementById("priceStart").textContent = "*";
            document.getElementById("priceStart").style.color = "red";
            monthlyPrice.style.borderColor = "red";
            isValid = false;
        }
        else {
            document.getElementById("priceStart").textContent = "";
            monthlyPrice.style.borderColor = "";
        }

        // Prevent form submission if validation fails
        if (!isValid) {
            event.preventDefault();
        }
    });

    // Display error message below the field
    function showError(fieldName, message) {
        var errorSpan = form.querySelector('[data-valmsg-for="' + fieldName + '"]');

        if (errorSpan != null) {
            errorSpan.textContent = message;
            errorSpan.classList.remove("field-validation-valid");
            errorSpan.classList.add("field-validation-error");
        }
    }
});
