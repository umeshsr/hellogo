the polycom phone on boot up is configured to contact the server and request a configuration file
the outbound from the phone is in this format:?	boot.ashoka.org/[USERID1]=[USERID2]=[NUMBEROFLINESUSERID2]/[KEY] (there is a url re-write on iis that converts this to params that are passed to the below)
the phone makes essentially two calls to the server
call one hits the boot_polycom_main file and this is use to write back a standard xml for the type of phone that is making the call. the template file is in the polycom folder called [phone model]-main-template. there are no variables in in this file and it is passed back to the phone as is
call two is used to configure the line(s) on the phone based on the params sent in the above call
the template file is in the polycome folder called [phone model]-line-template.
this file has a number of variables that set the line(s) of the phone to the appropriate user, set e911 location, etc.