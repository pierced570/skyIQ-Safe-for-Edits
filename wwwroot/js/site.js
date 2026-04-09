// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
//const { exec } = require('child_process');

//exec('python /python/test.py', (error, stdout, stderr) => {
//    console.log(stdout);
//})

//$(document).ready(function () {
//    alert("ready")
//    exec('python /python/test.py', (error, stdout, stderr) => {
//        alert(stdout);
//    })
//})

function openMenu() {
    document.getElementById("sideMenu").style.left = "0";
    document.getElementById("overlay").style.display = "block";
}

function closeMenu() {
    document.getElementById("sideMenu").style.left = "-280px";
    document.getElementById("overlay").style.display = "none";
}

/*
const dropzone = document.getElementById("dropzone");
const fileInput = document.getElementById("formFile");
const fileNameDisplay = document.getElementById("file-name");

dropzone.addEventListener("click", () => fileInput.click());

fileInput.addEventListener("change", (event) => {
fileNameDisplay.textContent = event.target.files.length
? event.target.files[0].name
: "No file chosen";
});

dropzone.addEventListener("dragover", (event) => {
event.preventDefault();
dropzone.classList.add("active");
});

dropzone.addEventListener("dragleave", () => {
dropzone.classList.remove("active");
});

dropzone.addEventListener("drop", (event) => {
event.preventDefault();
dropzone.classList.remove("active");
if (event.dataTransfer.files.length) {
fileInput.files = event.dataTransfer.files;
fileNameDisplay.textContent = fileInput.files[0].name;
}
});               */

jQuery(document).ready(function ($) {
    $('.trip-search').on('keyup', function () {
        var searchText = $(this).val().toLowerCase();

        $('.prev-trips .prev-trip').each(function () {
            var tripText = $(this).text().toLowerCase();
            if (tripText.includes(searchText)) {
                $(this).removeClass('hidden');
            } else {
                $(this).addClass('hidden');
            }
        });
    });
});

const fileInput = document.getElementById('fileInput');
const fileNameSpan = document.querySelector('.file-name');
const uploadBox = document.getElementById('uploadBox');

fileInput.addEventListener('change', () => {
    const file = fileInput.files[0];
    if (file) {
        fileNameSpan.textContent = file.name;
    }
});

function removeFile(event) {
    event.stopPropagation();
    fileInput.value = "";
    fileNameSpan.textContent = "Upload Itinerary HERE";
}

// Drag and Drop
uploadBox.addEventListener('dragover', (e) => {
    e.preventDefault();
    uploadBox.classList.add('drag-over');
});

uploadBox.addEventListener('dragleave', () => {
    uploadBox.classList.remove('drag-over');
});

uploadBox.addEventListener('drop', (e) => {
    e.preventDefault();
    uploadBox.classList.remove('drag-over');
    const files = e.dataTransfer.files;
    if (files.length > 0 && files[0].type === "application/pdf") {
        fileInput.files = files;
        fileNameSpan.textContent = files[0].name;
    }
});

$(document).ready(function () {
    var facts = [
        "skyIQ uses AI and a proprietary algorithm to parse flight plans in seconds—saving what used to take hours.",
        "We built skyIQ so pilots can stay focused on flying—not spreadsheets or fuel audits.",
        "Over 90% of charter trips contain inefficiencies that cost operators time and fuel.",
        "Nearly all charter trips with more than 2 legs are not optimally uptaking fuel—costing operators time, money, and efficiency.",
        "Commercial airlines began using “cost to carry” models in the early 2000s to reduce excess fuel burn and save millions annually.",
        "Carrying unused fuel can account for up to 4.5% of total burn—costing major airlines over $230M annually. skyIQ helps you avoid this hidden waste.",
        "skyIQ analyzes over 50 variables—including cost to carry—to optimize fuel stops and tankering.",
        "Airlines like Lufthansa and Delta use machine learning to optimize fueling. skyIQ brings that same power to charter operations—no dispatch department required.",
        "Every 1% saved in fuel burn can translate into thousands in annual savings per aircraft.",
        "Last-minute trip thrown your way? skyIQ helps you respond like a pro—and save money.",
        "skyIQ adapts in real time to fuel prices and dynamic itineraries—helping you preserve fee waivers.",
        "Missed fee waivers due to poor fuel planning can cost thousands. skyIQ helps you avoid them.",
        "Optimizing your trip shouldn’t feel like guesswork—or a time-consuming math problem. skyIQ makes it simple."
    ]

    var interval;
    $("#loadingModal").on("show.bs.modal", function () {
        var randNum = Math.floor(Math.random() * facts.length)
        $(".modal .fact").text(facts[randNum])
        interval = setInterval(function () {
            var randNum = Math.floor(Math.random() * facts.length)
            $(".modal .fact").text(facts[randNum])
        }, 8000);
    })
    $("#loadingModal").on("hide.bs.modal", function () {
        clearInterval(interval);
    })
})



$(document).on('change', '#StartingFuel', function () {
    $(this).next('.invalid-feedback').remove();
    if ($('#StartingFuel').val() > $('.aircraft-select option:selected').data('maxfuel')) {
        $('#StartingFuel').addClass('is-invalid');
        $('#StartingFuel').removeClass('is-valid');
        $('<div class="invalid-feedback" style="text-align:right;font-weight:600">')
            .text("Starting fuel exceeds capacity.")
            .insertAfter($('#StartingFuel'));
        $('.submit-btn').addClass('disabled');
    } else {
        $('#StartingFuel').removeClass('is-invalid');
        $('#StartingFuel').addClass('is-valid');
        $('.submit-btn').removeClass('disabled');
    }

});





$(document).on('change', '.fuel-burn-row input', function () {

    $('.submit-btn').removeClass('disabled');
    $('.fuel-burn-row .invalid-feedback').remove();

    $('.fuel-burn-row input').each(function () {
        $(this).removeClass('is-invalid');
        $(this).next('.invalid-feedback').remove();

        if (($('.aircraft-select option:selected').data('maxfuel') - $('.aircraft-select option:selected').data('reserve') - $(this).val()) <= 0) {
            $('.submit-btn').addClass('disabled');
            $(this).addClass('is-invalid');
            $('<div class="invalid-feedback" style="text-align:right;font-weight:600">')
                .text("Fuel burn exceeds reserve.")
                .insertAfter($(this));
        }
    });
});