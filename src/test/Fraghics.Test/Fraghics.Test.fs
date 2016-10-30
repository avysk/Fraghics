module Fraghics.Test

open NUnit.Framework
open FsUnit

[<Test>]
let ``Example Test`` () =
  2 |> Fraghics.Foo.f |> should equal 3

// vim: sw=2:sts=2:ai:foldmethod=indent:colorcolumn=80
