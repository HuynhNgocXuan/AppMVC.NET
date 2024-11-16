const gulp = require("gulp");
const cssmin = require("gulp-cssmin");
const rename = require("gulp-rename");
const sass = require("gulp-sass")(require("sass"));
const path = require("path");

function compileSass() {
  return (
    gulp
      .src(path.resolve(__dirname, "assets/scss/site.scss"), {
        allowEmpty: true,
      })
      .pipe(sass().on("error", sass.logError))
      // .pipe(cssmin())
      .pipe(
        rename({
          // suffix: ".min"
        })
      )
      .pipe(gulp.dest("wwwroot/css/"))
  );
}

function watchFiles() {
  gulp.watch("assets/scss/*.scss", compileSass);
}

exports.default = gulp.series(compileSass);
exports.watch = gulp.series(compileSass, watchFiles);
