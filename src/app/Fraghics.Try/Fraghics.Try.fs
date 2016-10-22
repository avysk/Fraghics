module Program
open Fraghics

[<EntryPoint>]
let main argv =
  G.OpenGraph ()
  //G.SetWindowTitle "Foobar!"

  G.ResizeWindow 600 600
//  G.Plot 100 100

//  G.SetColor 255 0 0
//  G.Plot 200 200

//  G.SetColor 0 255 0
//  for y=10 to 100 do
//    G.Plot 20 y
//  G.SetColor 0 0 255

//  let blues = [| for idx in 10..100 do yield (299, idx) |]
//  G.Plots blues

//  G.MoveTo 279 10
//  G.LineTo 279 100

  G.SetColor 120 200 20
//  G.MoveTo 200 200
//  G.LineTo 300 300
//  G.CurveTo (300, 400) (0, 0) (100, 500)
  for i = 1 to 200 do
    G.MoveTo i (2 * i)
    G.LineTo (2 * i) i

  G.Autosynchronize <- false
  G.SetColor 120 20 200
  for i = 1 to 200 do
    G.MoveTo (400 - i) (2 * i)
    G.LineTo (400 - 2 * i) i
  G.Synchronize()
  G.Run ()
  0

// vim: sw=2:sts=2:ai:foldmethod=indent:colorcolumn=80
